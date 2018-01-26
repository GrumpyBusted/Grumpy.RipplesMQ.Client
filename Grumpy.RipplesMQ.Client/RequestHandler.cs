using System;
using System.Threading;
using Grumpy.Common;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    /// <summary>
    /// Request Handler
    /// </summary>
    public sealed class RequestHandler : IDisposable
    {
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private IQueueHandler _queueHandler;
        private Func<object, object> _handler;
        private Func<object, CancellationToken, object> _cancelableHandler;
        private Type _requestType;
        private Type _responseType;
        private bool _multiThreaded;
        private bool _disposed;

        /// <summary>
        /// Request Name
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Queue Name
        /// </summary>
        public string QueueName { get; }

        /// <inheritdoc />
        public RequestHandler(MessageBusConfig messageBusConfig, IMessageBroker messageBroker, IQueueHandlerFactory queueHandlerFactory, string name)
        {
            _messageBroker = messageBroker;
            _queueHandlerFactory = queueHandlerFactory;
            Name = name;
            QueueName = $"{messageBusConfig.ServiceName.Replace("$", ".")}.{Name}.{UniqueKeyUtility.Generate()}";
        }

        /// <summary>
        /// Set Request Handler
        /// </summary>
        /// <param name="requestType">Request Dto Type</param>
        /// <param name="responseType">Response Dto Type</param>
        /// <param name="multiThreaded">Multi Threaded Request Handler</param>
        /// <param name="handler">´Call back method for handling request</param>
        public void Set(Type requestType, Type responseType, bool multiThreaded, Func<object, object> handler)
        {
            _handler = handler;

            Set(requestType, responseType, multiThreaded);
        }

        /// <summary>
        /// Set Request Handler
        /// </summary>
        /// <param name="requestType">Request Dto Type</param>
        /// <param name="responseType">Response Dto Type</param>
        /// <param name="multiThreaded">Multi Threaded Request Handler</param>
        /// <param name="handler">´Call back method for handling request</param>
        public void Set(Type requestType, Type responseType, bool multiThreaded, Func<object, CancellationToken, object> handler)
        {
            _cancelableHandler = handler;

            Set(requestType, responseType, multiThreaded);
        }

        /// <summary>
        /// Start handling requests
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="syncMode">Run Synchronous</param>
        public void Start(CancellationToken cancellationToken, bool syncMode)
        {
            if (_queueHandler == null)
                throw new ArgumentException("Cannot Start before Set");

            _messageBroker.RegisterRequestHandler(Name, QueueName, cancellationToken);
            _queueHandler.Start(QueueName, true, LocaleQueueMode.TemporaryMaster, true, MessageHandler, ErrorHandler, null, 1000, _multiThreaded, syncMode, cancellationToken);
        }
        
        /// <summary>
        /// Stop handling request
        /// </summary>
        public void Stop()
        {
            _queueHandler?.Stop();
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
                {
                    Stop();
                    _queueHandler?.Dispose();
                }

                _disposed = true;
            }
        }

        private void Set(Type requestType, Type responseType, bool multiThreaded)
        {
            if (_queueHandler != null)
                throw new ArgumentException("Cannot Set Handler Twice");

            _requestType = requestType;
            _responseType = responseType;
            _multiThreaded = multiThreaded;

            _queueHandler = _queueHandlerFactory.Create();
        }

        /// <summary>
        /// Handler for Request Message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public void MessageHandler(object message, CancellationToken cancellationToken)
        {
            if (message is RequestMessage requestMessage)
            {
                if (requestMessage.Body.GetType() != _requestType)
                    throw new InvalidMessageTypeException(message, _requestType, requestMessage.Body.GetType());

                var response = _handler != null ? _handler(requestMessage.Body) : _cancelableHandler(requestMessage.Body, cancellationToken);

                if (response.GetType() != _responseType)
                    throw new InvalidMessageTypeException(message, response, _responseType, message.GetType());

                _messageBroker.SendResponseMessage(requestMessage.ReplyQueue, requestMessage, response);
            }
            else
                throw new InvalidMessageTypeException(message, typeof(RequestMessage), message.GetType());
        }

        /// <summary>
        /// Error Handler for request Message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="exception">Exception</param>
        public void ErrorHandler(object message, Exception exception)
        {
            if (message is RequestMessage requestMessage)
                _messageBroker.SendResponseErrorMessage(requestMessage.ReplyQueue, requestMessage, exception);
            else
                throw new InvalidMessageTypeException(message, typeof(RequestMessage), message.GetType());
        }
    }
}