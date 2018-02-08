using System;
using System.Threading;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    /// <summary>
    /// Subscribe Handler
    /// </summary>
    public sealed class SubscribeHandler : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private IQueueHandler _queueHandler;
        private Action<object> _handler;
        private Action<object, CancellationToken> _cancelableHandler;
        private bool _multiThreaded;
        private bool _disposed;
        
        /// <summary>
        /// Subscriber Name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Topic to subscribe to
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// Message Type
        /// </summary>
        public Type MessageType { get; private set; }

        /// <summary>
        /// Queue Name
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Is Subscribe Handler Durable
        /// </summary>
        public bool Durable { get; }

        /// <inheritdoc />
        public SubscribeHandler(ILogger logger, IMessageBroker messageBroker, IQueueHandlerFactory queueHandlerFactory, string name, string topic, bool durable, IQueueNameUtility queueNameUtility)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _queueHandlerFactory = queueHandlerFactory;
            Name = name;
            Topic = topic;
            Durable = durable;
            QueueName = queueNameUtility.Build(Topic, Name, Durable);
        }

        /// <summary>
        /// Set Subscribe Handler
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="multiThreaded">Is handler method Thread safe</param>
        /// <param name="handler">Call back method for handler</param>
        public void Set(Type messageType, bool multiThreaded, Action<object> handler)
        {
            _handler = handler;
         
            Set(messageType, multiThreaded);
        }

        /// <summary>
        /// Set Subscribe Handler
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="multiThreaded">Is handler method Thread safe</param>
        /// <param name="handler">Call back method for handler</param>
        public void Set(Type messageType, bool multiThreaded, Action<object, CancellationToken> handler)
        {
            _cancelableHandler = handler;

            Set(messageType, multiThreaded);
        }

        /// <summary>
        /// Start handling messages
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="syncMode">Run Synchronous</param>
        public void Start(CancellationToken cancellationToken, bool syncMode)
        {
            if (_queueHandler == null)
                throw new ArgumentException("Cannot Start before Set");

            _queueHandler.Start(QueueName, true, Durable ? LocaleQueueMode.DurableCreate : LocaleQueueMode.TemporaryMaster, true, MessageHandler, (o, exception) => ErrorHandler(o, exception), null, 1000, _multiThreaded, syncMode, cancellationToken);

            _logger.Information("Subscribe Handler started {@SubscribeHandler}", this);
        }

        /// <summary>
        /// Stop handling messages
        /// </summary>
        public void Stop()
        {
            _queueHandler?.Stop();

            _logger.Information("Subscribe Handler stopped {@SubscribeHandler}", this);
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
                _disposed = true;

                if (disposing)
                {
                    Stop();

                    _queueHandler?.Dispose();
                }
            }
        }

        private void Set(Type messageType, bool multiThreaded)
        {
            if (_queueHandler != null)
                throw new ArgumentException("Cannot Set Handler Twice");

            MessageType = messageType;
            _multiThreaded = multiThreaded;

            _queueHandler = _queueHandlerFactory.Create();
        }

        /// <summary>
        /// Handle Subscribe Message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public void MessageHandler(object message, CancellationToken cancellationToken)
        {
            _logger.Debug("Subscribe Handler received message {@SubscribeHandler} {@Message} {Type}", this, message, message.GetType().FullName);

            if (message is PublishMessage publishMessage)
            {
                if (publishMessage.MessageType != MessageType.ToString())
                    throw new InvalidMessageTypeException(publishMessage, MessageType, message.GetType());

                if (_handler != null)
                    _handler(JsonConvert.DeserializeObject(publishMessage.MessageBody, MessageType));
                else
                    _cancelableHandler(JsonConvert.DeserializeObject(publishMessage.MessageBody, MessageType), cancellationToken);

                _messageBroker.SendSubscribeHandlerCompletedMessage(Name, publishMessage);
            }
            else
                throw new InvalidMessageTypeException(message, typeof(RequestMessage), message.GetType());
        }

        /// <summary>
        /// Error Handler for Subscribe Message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception</param>
        public void ErrorHandler(object message, Exception exception)
        {
            _logger.Warning(exception, "Subscribe Handler received message in Error Handler {@SubscribeHandler} {@Message} {Type}", this, message, message.GetType().FullName);

            if (message is PublishMessage publishMessage)
                _messageBroker.SendSubscribeHandlerErrorMessage(Name, Durable, publishMessage, exception);
            else
                throw new InvalidMessageTypeException(message, typeof(PublishMessage), message.GetType());
        }
    }
}