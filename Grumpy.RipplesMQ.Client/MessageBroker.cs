using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Config;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    public class MessageBroker : IMessageBroker
    {
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;
        private readonly string _queueNamePrefix;
        private readonly ILocaleQueue _messageBrokerQueue;
        private bool _disposed;

        /// <inheritdoc />
        public MessageBroker(MessageBusConfig messageBusConfig, IQueueFactory queueFactory, IProcessInformation processInformation)
        {
            _messageBusConfig = messageBusConfig;
            _queueFactory = queueFactory;
            _processInformation = processInformation;

            _queueNamePrefix = $"{_messageBusConfig.ServiceName.Replace("$", ".")}";
            _messageBrokerQueue = _queueFactory.CreateLocale(MessageBrokerConfig.LocaleQueueName, true, LocaleQueueMode.Durable, true);

            if (!_messageBrokerQueue.Exists())
                throw new MessageBrokerException(MessageBrokerConfig.LocaleQueueName);
        }

        /// <inheritdoc />
        public MessageBusServiceRegisterReplyMessage RegisterMessageBusService(CancellationToken cancellationToken)
        {
            var messageBusServiceRegisterMessage = new MessageBusServiceRegisterMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                ReplyQueue = $"{_queueNamePrefix}.{typeof(MessageBusServiceRegisterMessage).Name}.Reply.{UniqueKeyUtility.Generate()}",
                RegisterDateTime = DateTimeOffset.Now
            };

            using (var replyQueue = _queueFactory.CreateLocale(messageBusServiceRegisterMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false))
            {
                _messageBrokerQueue.Send(messageBusServiceRegisterMessage);

                var messageBusServiceRegisterReplyMessage = replyQueue.Receive<MessageBusServiceRegisterReplyMessage>(10000, cancellationToken);

                if (messageBusServiceRegisterReplyMessage == null)
                    throw new MessageBusServiceRegisterTimeoutException(messageBusServiceRegisterMessage);

                messageBusServiceRegisterReplyMessage.CompletedDateTime = DateTimeOffset.Now;

                return messageBusServiceRegisterReplyMessage;
            }
        }

        /// <inheritdoc />
        public SubscribeHandlerRegisterReplyMessage RegisterSubscribeHandler(string name, string topic, bool durable, string queueName, CancellationToken cancellationToken)
        {
            var messageBusSubscriberRegisterMessage = new SubscribeHandlerRegisterMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                Name = name,
                Topic = topic,
                Durable = durable,
                QueueName = queueName,
                ReplyQueue = $"{_queueNamePrefix}.{typeof(SubscribeHandlerRegisterMessage).Name}.Reply.{UniqueKeyUtility.Generate()}",
                RegisterDateTime = DateTimeOffset.Now
            };

            using (var replyQueue = _queueFactory.CreateLocale(messageBusSubscriberRegisterMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false))
            {
                _messageBrokerQueue.Send(messageBusSubscriberRegisterMessage);

                var subscribeHandlerRegisterReplyMessage = replyQueue.Receive<SubscribeHandlerRegisterReplyMessage>(10000, cancellationToken);

                if (subscribeHandlerRegisterReplyMessage == null)
                    throw new SubscribeHandlerRegisterTimeoutException(messageBusSubscriberRegisterMessage);

                subscribeHandlerRegisterReplyMessage.CompletedDateTime = DateTimeOffset.Now;

                return subscribeHandlerRegisterReplyMessage;
            }
        }

        /// <inheritdoc />
        public RequestHandlerRegisterReplyMessage RegisterRequestHandler(string name, string queueName, CancellationToken cancellationToken)
        {
            var requestHandlerRegisterMessage = new RequestHandlerRegisterMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                Name = name,
                QueueName = queueName,
                ReplyQueue = $"{_queueNamePrefix}.{typeof(RequestHandlerRegisterMessage).Name}.Reply.{UniqueKeyUtility.Generate()}",
                RegisterDateTime = DateTimeOffset.Now
            };

            using (var replyQueue = _queueFactory.CreateLocale(requestHandlerRegisterMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false))
            {
                _messageBrokerQueue.Send(requestHandlerRegisterMessage);

                var subscribeHandlerRegisterReplyMessage = replyQueue.Receive<RequestHandlerRegisterReplyMessage>(10000, cancellationToken);

                if (subscribeHandlerRegisterReplyMessage == null)
                    throw new RequestHandlerRegisterTimeoutException(requestHandlerRegisterMessage);

                subscribeHandlerRegisterReplyMessage.CompletedDateTime = DateTimeOffset.Now;

                return subscribeHandlerRegisterReplyMessage;
            }
        }

        /// <inheritdoc />
        public void SendMessageBusHandshake(IEnumerable<Shared.Messages.SubscribeHandler> subscribeHandlers, IEnumerable<Shared.Messages.RequestHandler> requestHandlers)
        {
            var requestHandlerRegisterMessage = new MessageBusServiceHandshakeMessage
            {
                ServerName = _processInformation.MachineName,
                ServiceName = _messageBusConfig.ServiceName,
                HandshakeDateTime = DateTimeOffset.Now,
                SubscribeHandlers = subscribeHandlers,
                RequestHandlers = requestHandlers
            };

            _messageBrokerQueue.Send(requestHandlerRegisterMessage);
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
                Body = message,
                ReplyQueue = persistent ? $"{_queueNamePrefix}.{topic}.{typeof(PublishMessage).Name}.Reply.{id}" : null,
                PublishDateTime = DateTimeOffset.Now,
                ErrorCount = 0
            };

            PublishReplyMessage replyMessage;

            if (persistent)
            {
                using (var replyQueue = _queueFactory.CreateLocale(publishMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, false))
                {
                    _messageBrokerQueue.Send(publishMessage);

                    replyMessage = replyQueue.Receive<PublishReplyMessage>(3000, cancellationToken);

                    if (replyMessage == null)
                        throw new PublishReplyTimeoutException(publishMessage);
                }
            }
            else
            {
                _messageBrokerQueue.Send(publishMessage);

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
                MessageType = publishMessage.Body.GetType(),
                Persistent = publishMessage.Persistent,
                PublishDateTime = publishMessage.PublishDateTime,
                HandlerDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            _messageBrokerQueue.Send(subscriberCompleteMessage);
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

            _messageBrokerQueue.Send(subscriberErrorMessage);
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
                Body = request,
                ReplyQueue = $"{_queueNamePrefix}.{name}.{typeof(RequestMessage).Name}.Reply.{UniqueKeyUtility.Generate()}",
                RequestDateTime = DateTimeOffset.Now
            };

            ITransactionalMessage message;

            using (var queue = _queueFactory.CreateLocale(requestMessage.ReplyQueue, true, LocaleQueueMode.TemporaryMaster, true))
            {
                _messageBrokerQueue.Send(requestMessage);

                message = await queue.ReceiveAsync(millisecondsTimeout, cancellationToken);
            }

            switch (message?.Message)
            {
                case null:
                    throw new RequestResponseTimeoutException(requestMessage, millisecondsTimeout);

                case ResponseMessage responseMessage:
                    responseMessage.CompletedDateTime = DateTimeOffset.Now;

                    if (responseMessage.Body is TResponse response)
                        return response;

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
                Body = response,
                RequestDateTime = requestMessage.RequestDateTime,
                ResponseDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };

            if (responseMessage.RequesterServerName == responseMessage.ResponderServerName)
            {
                using (var queue = _queueFactory.CreateLocale(responseMessage.ReplyQueue, true, LocaleQueueMode.TemporarySlave, true))
                {
                    queue.Send(responseMessage);
                }
            }
            else 
                _messageBrokerQueue.Send(responseMessage);
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

            _messageBrokerQueue.Send(responseErrorMessage);
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
    }
}