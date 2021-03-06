using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Threading;
using Grumpy.Json;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Config;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    /// <inheritdoc />
    /// <summary>
    /// Test Version of The Message Broker - This override some features to be able to capture test of a Service using the Message Bus. This includes features for sending requests and publish messages that should be handled in the services. 
    /// </summary>
    public sealed class TestMessageBroker : IMessageBroker
    {
        private readonly IQueueFactory _queueFactory;
        private readonly MessageBroker _messageBroker;
        private readonly List<SubscribeHandlerCompleteMessage> _subscribeHandlerCompleted = new List<SubscribeHandlerCompleteMessage>();
        private readonly List<SubscribeHandlerErrorMessage> _subscribeHandlerFailed = new List<SubscribeHandlerErrorMessage>();
        private readonly List<PublishMessage> _published = new List<PublishMessage>();
        private readonly List<ResponseMessage> _responses = new List<ResponseMessage>();
        private readonly Dictionary<string, List<string>> _topicSubscribers = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, ILocaleQueue> _subscriberQueues = new Dictionary<string, ILocaleQueue>();
        private readonly Dictionary<string, List<RequestMock>> _requestMocks = new Dictionary<string, List<RequestMock>>();
        private readonly Dictionary<string, ILocaleQueue> _requestQueues = new Dictionary<string, ILocaleQueue>();
        private readonly object _lock = new object();
        private int _pendingMessages;
        private bool _first = true;

        /// <summary>
        /// Collection of Messages published to the Test Message Bus, Use the for asserting that the messages was published as expected in the Service.
        /// </summary>
        public IEnumerable<PublishMessage> Published => _published;

        /// <summary>
        /// Collection of Successful ended Subscribe Handlers, use for asserting in Test Cases
        /// </summary>
        public IEnumerable<SubscribeHandlerCompleteMessage> SubscriberHandlerCompleted => _subscribeHandlerCompleted;

        /// <summary>
        /// Collection of Failed Subscribe Handlers, use for asserting in Test Cases
        /// </summary>
        public IEnumerable<SubscribeHandlerErrorMessage> SubscriberHandlerFailed => _subscribeHandlerFailed;

        /// <summary>
        /// Message Bus that will return the Test Message Bus, use for Dependency Injection into services
        /// </summary>
        public IMessageBus MessageBus { get; }

        /// <inheritdoc />
        public TestMessageBroker()
        {
            var logger = NullLogger.Instance;

            var messageBusConfig = new MessageBusConfig
            {
                ServiceName = "TestRipplesMQ"
            };

            _queueFactory = new TestQueueFactory(this);

            var processInformation = new ProcessInformation();
            var queueNameUtility = new QueueNameUtility(messageBusConfig.ServiceName);

            _messageBroker = new MessageBroker(logger, messageBusConfig, _queueFactory, processInformation, queueNameUtility);

            var queueHandlerFactory = new QueueHandlerFactory(logger, _queueFactory);

            MessageBus = new MessageBus(logger, this, queueHandlerFactory, queueNameUtility);
        }

        /// <summary>
        /// Publish Message, use the to simulate a Publish event from another service. This will trigger the appropriate registered subscribers
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="message">Message to Publish</param>
        /// <typeparam name="T">Type of Message</typeparam>
        public void Publish<T>(PublishSubscribeConfig config, T message)
        {
            foreach (var topicSubscribers in _topicSubscribers)
            {
                if (topicSubscribers.Key == config.Topic)
                {
                    foreach (var queueName in topicSubscribers.Value)
                    {
                        var queue = _subscriberQueues.FirstOrDefault(s => s.Key == queueName);

                        MockMessage(queue.Value, CreatePublishMessage(config, message), true);

                        lock (_lock)
                        {
                            ++_pendingMessages;
                        }

                        WaitForIt();
                    }
                }
            }
        }

        /// <summary>
        /// Mock a response from a request handler in another service, use this for handling any request that user services need to be tested
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="function">Selector function, return true to response with this response message</param>
        /// <param name="response">Response Message</param>
        /// <typeparam name="TRequest">Type of request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response Message</typeparam>
        public void MockResponse<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, bool> function, TResponse response)
        {
            if (!_requestMocks.ContainsKey(config.Name))
                _requestMocks.Add(config.Name, new List<RequestMock>());

            _requestMocks[config.Name].Add(new RequestMock { Function = a => function((TRequest)a), Response = response, ResponseType = typeof(TResponse) });
        }

        /// <summary>
        /// Mock a response from a request handler in another service, use this for handling any request that user services need to be tested. This is a default Responses in all cases. Add this last if you have multiple mocks for same request.
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="response">Response Message</param>
        /// <typeparam name="TResponse">Type of Response Message</typeparam>
        public void MockResponse<TResponse>(RequestResponseConfig config, TResponse response)
        {
            if (!_requestMocks.ContainsKey(config.Name))
                _requestMocks.Add(config.Name, new List<RequestMock>());

            _requestMocks[config.Name].Add(new RequestMock { Function = a => true, Response = response, ResponseType = typeof(TResponse) });
        }

        /// <summary>
        /// Request a responses from your service, this will invoke the request handler registered on your service as if it was called from another service.<br/>
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="request">Request message</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response Message</typeparam>
        /// <returns>Response Message</returns>
        public TResponse Request<TRequest, TResponse>(RequestResponseConfig config, TRequest request)
        {
            var queue = _requestQueues.FirstOrDefault(q => q.Key == config.Name);

            var replyQueue = UniqueKeyUtility.Generate();

            if (queue.Value != null)
            {
                MockMessage(queue.Value, new RequestMessage
                {
                    MessageBody = SerializeToJson(request),
                    RequestType = typeof(TRequest).FullName,
                    ResponseType = typeof(TResponse).FullName,
                    Name = config.Name,
                    ReplyQueue = replyQueue
                }, true);

                // ReSharper disable once ImplicitlyCapturedClosure
                TimerUtility.WaitForIt(() => _responses.Any(r => r.ReplyQueue == replyQueue), Debugger.IsAttached ? 360000 : config.MillisecondsTimeout);
            }

            Thread.Sleep(100);

            var response = _responses.FirstOrDefault(r => r.ReplyQueue == replyQueue);

            return JsonConvert.DeserializeObject<TResponse>(response?.MessageBody);
        }

        /// <inheritdoc />
        public MessageBusServiceHandshakeReplyMessage SendMessageBusHandshake(IEnumerable<Shared.Messages.SubscribeHandler> subscribeHandlers, IEnumerable<Shared.Messages.RequestHandler> requestHandlers, CancellationToken cancellationToken)
        {
            var subscribeHandlerList = subscribeHandlers.ToList();
            var requestHandlerList = requestHandlers.ToList();

            if (_first)
            {
                _first = false;

                foreach (var subscribeHandler in subscribeHandlerList)
                {
                    if (!_topicSubscribers.ContainsKey(subscribeHandler.Topic))
                        _topicSubscribers.Add(subscribeHandler.Topic, new List<string>());

                    _topicSubscribers[subscribeHandler.Topic].Add(subscribeHandler.QueueName);

                    _subscriberQueues[subscribeHandler.QueueName] = MockQueue(subscribeHandler.QueueName);
                }

                foreach (var requestHandler in requestHandlerList)
                {
                    _requestQueues[requestHandler.Name] = MockQueue(requestHandler.QueueName);
                }
            }

            MockMessage($".Reply.{typeof(MessageBusServiceHandshakeMessage).Name}.", new MessageBusServiceHandshakeReplyMessage());

            return _messageBroker.SendMessageBusHandshake(subscribeHandlerList, requestHandlerList, cancellationToken);
        }

        /// <inheritdoc />
        public PublishReplyMessage SendPublishMessage<T>(string topic, T message, bool persistent, CancellationToken cancellationToken)
        {
            if (persistent)
                MockMessage($".Reply.{typeof(PublishMessage).Name}.", new PublishReplyMessage());

            Publish(new PublishSubscribeConfig { Persistent = persistent, Topic = topic }, message);

            return _messageBroker.SendPublishMessage(topic, message, persistent, cancellationToken);
        }

        /// <inheritdoc />
        public void SendSubscribeHandlerCompletedMessage(string name, PublishMessage publishMessage)
        {
            _messageBroker.SendSubscribeHandlerCompletedMessage(name, publishMessage);

            lock (_lock)
            {
                --_pendingMessages;
            }
        }

        /// <inheritdoc />
        public void SendSubscribeHandlerErrorMessage(string name, bool durable, PublishMessage publishMessage, Exception exception)
        {
            _messageBroker.SendSubscribeHandlerErrorMessage(name, durable, publishMessage, exception);

            lock (_lock)
            {
                --_pendingMessages;
            }
        }

        /// <inheritdoc />
        public Task<TResponse> RequestResponseAsync<TRequest, TResponse>(string name, TRequest request, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (_requestMocks.ContainsKey(name))
            {
                var requestMock = _requestMocks[name]?.FirstOrDefault(a => a.ResponseType == typeof(TResponse) && a.Function(request));

                return Task.FromResult((TResponse)requestMock?.Response);
            }

            throw new RequestResponseTimeoutException(new RequestMessage
            {
                MessageBody = SerializeToJson(request),
                RequestType = typeof(TRequest).FullName,
                ResponseType = typeof(TResponse).FullName,
                Name = name
            }, millisecondsTimeout);
        }

        /// <inheritdoc />
        public void SendResponseMessage(string replyQueue, RequestMessage requestMessage, object response)
        {
            _messageBroker.SendResponseMessage(replyQueue, requestMessage, response);

            lock (_lock)
            {
                --_pendingMessages;
            }
        }

        /// <inheritdoc />
        public void SendResponseErrorMessage(string replyQueue, RequestMessage requestMessage, Exception exception)
        {
            _messageBroker.SendResponseErrorMessage(replyQueue, requestMessage, exception);

            lock (_lock)
            {
                --_pendingMessages;
            }
        }

        /// <inheritdoc />
        public void CheckMessageBrokerQueue()
        {
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        public void Dispose()
        {
            _messageBroker?.Dispose();
        }

        internal void RegisterPublish(PublishMessage message)
        {
            _published.Add(message);
        }

        internal void RegisterSubscriberComplete(SubscribeHandlerCompleteMessage message)
        {
            _subscribeHandlerCompleted.Add(message);
        }

        internal void RegisterSubscribeError(SubscribeHandlerErrorMessage message)
        {
            _subscribeHandlerFailed.Add(message);
        }

        internal void RegisterResponse(ResponseMessage message)
        {
            _responses.Add(message);
        }

        private static PublishMessage CreatePublishMessage<T>(PublishSubscribeConfig publishSubscribeConfig, T message)
        {
            return new PublishMessage
            {
                MessageBody = SerializeToJson(message),
                MessageType = typeof(T).FullName,
                MessageId = UniqueKeyUtility.Generate(),
                Persistent = publishSubscribeConfig.Persistent,
                ReplyQueue = null,
                Topic = publishSubscribeConfig.Topic
            };
        }

        private void WaitForIt()
        {
            TimerUtility.WaitForIt(() => _pendingMessages == 0, Debugger.IsAttached ? 3600000 : 6000);
        }

        private ILocaleQueue MockQueue(string name)
        {
            return _queueFactory.CreateLocale(name, true, LocaleQueueMode.TemporaryMaster, true, AccessMode.SendAndReceive);
        }

        private static void MockMessage<T>(IQueue queue, T message, bool onlyOnce)
        {
            if (queue is TestQueue testQueue)
                testQueue.SetMessage(message, onlyOnce);
        }

        private void MockMessage<T>(string queueName, T message)
        {
            MockMessage(MockQueue(queueName), message, true);
        }

        private static string SerializeToJson(object response)
        {
            var jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            return response.SerializeToJson(jsonSerializerSettings);
        }
    }
}