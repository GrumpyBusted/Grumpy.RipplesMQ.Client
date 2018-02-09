using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Interfaces;
using Grumpy.Json;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Config;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    public class MessageBroker : IMessageBroker
    {
        private readonly ILogger _logger;
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;
        private readonly ILocaleQueue _messageBrokerQueue;
        private readonly IQueueNameUtility _queueNameUtility;
        private bool _disposed;

        /// <inheritdoc />
        public MessageBroker(ILogger logger, MessageBusConfig messageBusConfig, IQueueFactory queueFactory, IProcessInformation processInformation, IQueueNameUtility queueNameUtility)
        {
            _logger = logger;
            _messageBusConfig = messageBusConfig;
            _queueFactory = queueFactory;
            _processInformation = processInformation;
            _queueNameUtility = queueNameUtility;
            _messageBrokerQueue = _queueFactory.CreateLocale(MessageBrokerConfig.LocaleQueueName, true, LocaleQueueMode.Durable, true, AccessMode.Send);
        }

        /// <inheritdoc />
        public MessageBusServiceHandshakeReplyMessage SendMessageBusHandshake(IEnumerable<Shared.Messages.SubscribeHandler> subscribeHandlers, IEnumerable<Shared.Messages.RequestHandler> requestHandlers, CancellationToken cancellationToken)
        {
            var messageBusServiceHandshakeMessage = new MessageBusServiceHandshakeMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                SendDateTime = DateTimeOffset.Now,
                SubscribeHandlers = subscribeHandlers,
                RequestHandlers = requestHandlers,
                ReplyQueue = _queueNameUtility.ReplyQueue<MessageBusServiceHandshakeMessage>()
            };

            using (var replyQueue = _queueFactory.CreateLocale(messageBusServiceHandshakeMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false, AccessMode.Receive))
            {
                _logger.Debug("Message Broker Client sending Message Bus Handshake {@Message}", messageBusServiceHandshakeMessage);

                SendToMessageBroker(messageBusServiceHandshakeMessage);

                var messageBusServiceHandshakeReplyMessage = replyQueue.Receive<MessageBusServiceHandshakeReplyMessage>(30000, cancellationToken);

                if (messageBusServiceHandshakeReplyMessage == null)
                    throw new MessageBusHandshakeTimeoutException(messageBusServiceHandshakeMessage);

                messageBusServiceHandshakeReplyMessage.CompletedDateTime = DateTimeOffset.Now;

                _logger.Debug("Message Broker Client has received reply for Message bus Handshake {@ReplyMessage}", messageBusServiceHandshakeReplyMessage);

                return messageBusServiceHandshakeReplyMessage;
            }
        }

        /// <inheritdoc />
        public PublishReplyMessage SendPublishMessage<T>(string topic, T message, bool persistent, CancellationToken cancellationToken)
        {
            var id = UniqueKeyUtility.Generate();

            var publishMessage = new PublishMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                MessageId = id,
                Topic = topic,
                Persistent = persistent,
                MessageBody = SerializeToJson(message),
                MessageType = typeof(T).FullName,
                ReplyQueue = persistent ? _queueNameUtility.ReplyQueue<PublishMessage>(id) : null,
                PublishDateTime = DateTimeOffset.Now,
                ErrorCount = 0
            };

            PublishReplyMessage replyMessage;

            _logger.Debug("Message Broker Client sending Publish Message {@Message}", publishMessage);

            if (persistent)
            {
                using (var replyQueue = _queueFactory.CreateLocale(publishMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false, AccessMode.Receive))
                {
                    SendToMessageBroker(publishMessage);

                    replyMessage = replyQueue.Receive<PublishReplyMessage>(3000, cancellationToken);

                    if (replyMessage == null)
                        throw new PublishReplyTimeoutException(publishMessage);
                }
            }
            else
            {
                SendToMessageBroker(publishMessage);

                replyMessage = new PublishReplyMessage
                {
                    PublishDateTime = publishMessage.PublishDateTime,
                    MessageId = publishMessage.MessageId,
                    Topic = publishMessage.Topic
                };
            }

            replyMessage.CompletedDateTime = DateTimeOffset.Now;

            return replyMessage;
        }

        /// <inheritdoc />
        public void SendSubscribeHandlerCompletedMessage(string name, PublishMessage publishMessage)
        {
            var subscriberCompleteMessage = new SubscribeHandlerCompleteMessage
            {
                PublisherServerName = publishMessage.ServerName,
                PublisherServiceName = publishMessage.ServiceName,
                HandlerServerName = _processInformation.MachineName,
                HandlerServiceName = _messageBusConfig.ServiceName,
                MessageId = publishMessage.MessageId,
                Name = name,
                Topic = publishMessage.Topic,
                MessageType = publishMessage.MessageType,
                Persistent = publishMessage.Persistent,
                PublishDateTime = publishMessage.PublishDateTime,
                HandlerDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            SendToMessageBroker(subscriberCompleteMessage);
        }

        /// <inheritdoc />
        public void SendSubscribeHandlerErrorMessage(string name, bool durable, PublishMessage publishMessage, Exception exception)
        {
            var subscriberErrorMessage = new SubscribeHandlerErrorMessage
            {
                PublisherServerName = publishMessage.ServerName,
                PublisherServiceName = publishMessage.ServiceName,
                HandlerServerName = _processInformation.MachineName,
                HandlerServiceName = _messageBusConfig.ServiceName,
                MessageId = publishMessage.MessageId,
                Name = name,
                Durable = durable,
                Message = publishMessage,
                Exception = exception,
                PublishDateTime = publishMessage.PublishDateTime,
                HandlerDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            SendToMessageBroker(subscriberErrorMessage);
        }

        /// <inheritdoc />
        public async Task<TResponse> RequestResponseAsync<TRequest, TResponse>(string name, TRequest request, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var requestMessage = new RequestMessage
            {
                RequesterServerName = _processInformation.MachineName,
                RequesterServiceName = _messageBusConfig.ServiceName,
                RequestId = UniqueKeyUtility.Generate(),
                Name = name,
                MessageBody = SerializeToJson(request),
                RequestType = typeof(TRequest).FullName,
                ResponseType = typeof(TResponse).FullName,
                ReplyQueue = _queueNameUtility.ReplyQueue<RequestMessage>(name),
                RequestDateTime = DateTimeOffset.Now
            };

            ITransactionalMessage message;

            using (var queue = _queueFactory.CreateLocale(requestMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, true, AccessMode.Receive))
            {
                SendToMessageBroker(requestMessage);

                message = await queue.ReceiveAsync(millisecondsTimeout, cancellationToken);
            }

            switch (message?.Message)
            {
                case null:
                    throw new RequestResponseTimeoutException(requestMessage, millisecondsTimeout);

                case ResponseMessage responseMessage:
                    responseMessage.CompletedDateTime = DateTimeOffset.Now;

                    if (responseMessage.MessageType == null && responseMessage.MessageBody == "null")
                        return default(TResponse);
                    if (responseMessage.MessageType == typeof(TResponse).FullName)
                        return JsonConvert.DeserializeObject<TResponse>(responseMessage.MessageBody);

                    throw new InvalidMessageTypeException(requestMessage, responseMessage, typeof(TResponse), responseMessage.GetType());

                case ResponseErrorMessage responseErrorMessage:
                    responseErrorMessage.CompletedDateTime = DateTimeOffset.Now;

                    throw new RequestHandlerException(requestMessage, responseErrorMessage);
            }

            throw new InvalidMessageTypeException(requestMessage, typeof(ResponseMessage), message.GetType());
        }

        /// <inheritdoc />
        public void SendResponseMessage(string replyQueue, RequestMessage requestMessage, object response)
        {
            var responseMessage = new ResponseMessage
            {
                RequesterServerName = requestMessage.RequesterServerName,
                RequesterServiceName = requestMessage.RequesterServiceName,
                ResponderServerName = _processInformation.MachineName,
                ResponderServiceName = _messageBusConfig.ServiceName,
                RequestId = requestMessage.RequestId,
                ReplyQueue = replyQueue,
                MessageBody = SerializeToJson(response),
                MessageType = response?.GetType().FullName,
                RequestDateTime = requestMessage.RequestDateTime,
                ResponseDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            if (responseMessage.RequesterServerName == responseMessage.ResponderServerName)
            {
                try
                {
                    using (var queue = _queueFactory.CreateLocale(responseMessage.ReplyQueue, true, LocaleQueueMode.TemporarySlave, true, AccessMode.Send))
                    {
                        queue.Send(responseMessage);
                    }
                }
                catch
                {
                    SendToMessageBroker(responseMessage);
                }
            }
            else
                SendToMessageBroker(responseMessage);
        }

        private static string SerializeToJson(object response)
        {
            var jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            return response.SerializeToJson(jsonSerializerSettings);
        }

        /// <inheritdoc />
        public void SendResponseErrorMessage(string replyQueue, RequestMessage requestMessage, Exception exception)
        {
            var responseErrorMessage = new ResponseErrorMessage
            {
                RequesterServerName = requestMessage.RequesterServerName,
                RequesterServiceName = requestMessage.RequesterServiceName,
                ResponderServerName = _processInformation.MachineName,
                ResponderServiceName = _messageBusConfig.ServiceName,
                RequestId = requestMessage.RequestId,
                ReplyQueue = replyQueue,
                RequestMessage = requestMessage,
                Exception = exception,
                RequestDateTime = requestMessage.RequestDateTime,
                ResponseDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            SendToMessageBroker(responseErrorMessage);
        }

        /// <inheritdoc />
        public void CheckServer()
        {
            if (!_messageBrokerQueue.Exists())
                throw new MessageBrokerException(_messageBrokerQueue.Name);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _messageBrokerQueue?.Dispose();

                _disposed = true;
            }
        }

        private void SendToMessageBroker<T>(T message)
        {
            try
            {
                _messageBrokerQueue.Send(message);
            }
            catch (Exception exception)
            {
                _logger.Information(exception, "Exception sending message from Message Broker Client to Server, retrying once {@Message}", message);

                _messageBrokerQueue.Reconnect();

                try
                {
                    _messageBrokerQueue.Send(message);
                }
                catch (Exception innerException)
                {
                    throw new MessageBrokerException(innerException);
                }
            }
        }
    }
}