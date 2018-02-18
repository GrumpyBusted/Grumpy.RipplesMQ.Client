using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.Common.Threading;
using Grumpy.Logging;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Config;
using Microsoft.Extensions.Logging;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class MessageBus : IMessageBus
    {
        private readonly ILogger _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly List<SubscribeHandler> _subscribeHandlers;
        private readonly List<RequestHandler> _requestHandlers;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private bool _disposed;
        private TimerTask _handshakeTask;
        internal bool SyncMode = false;
        private readonly IQueueNameUtility _queueNameUtility;
        private int _reconnectCount = 0;

        /// <inheritdoc />
        public MessageBus(ILogger logger, IMessageBroker messageBroker, IQueueHandlerFactory queueHandlerFactory, IQueueNameUtility queueNameUtility)
        {
            _messageBroker = messageBroker;
            _queueHandlerFactory = queueHandlerFactory;
            _queueNameUtility = queueNameUtility;
            _logger = logger;

            _subscribeHandlers = new List<SubscribeHandler>();
            _requestHandlers = new List<RequestHandler>();
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            _logger.Information("Message Bus starting");

            if (_cancellationTokenSource != null)
                throw new ArgumentException("Message Bus not Stopped");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenRegistration = cancellationToken.Register(Stop);

            SendHandshake();

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var subscribeHandler in _subscribeHandlers)
            {
                subscribeHandler.Start(_cancellationTokenSource.Token, SyncMode);
            }

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var requestHandler in _requestHandlers)
            {
                requestHandler.Start(_cancellationTokenSource.Token, SyncMode);
            }

            _handshakeTask = new TimerTask();

            if (!SyncMode)
                _handshakeTask.Start(SendHandshake, 30000, _cancellationTokenSource.Token);

            _logger.Information("Message Bus started");
        }

        /// <inheritdoc />
        public void Stop()
        {
            _logger.Information("Message Bus stopping");

            try
            {
                _messageBroker.SendMessageBusHandshake(Enumerable.Empty<Shared.Messages.SubscribeHandler>(), Enumerable.Empty<Shared.Messages.RequestHandler>(), _cancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unable to send empty handshake to broker");
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _handshakeTask?.Dispose();

            Parallel.ForEach(_subscribeHandlers, subscribeHandler => subscribeHandler.Stop());
            Parallel.ForEach(_requestHandlers, requestHandler => requestHandler.Stop());

            _cancellationTokenRegistration.Dispose();

            _logger.Information("Message Bus stopped");
        }

        private void SendHandshake()
        {
            try
            {
                _messageBroker.CheckMessageBrokerQueue();

                var subscribeHandlers = _subscribeHandlers.Select(s => new Shared.Messages.SubscribeHandler { Name = s.Name, QueueName = s.QueueName, Topic = s.Topic, Durable = s.Durable, MessageType = s.MessageType.FullName });
                var requestHandlers = _requestHandlers.Select(s => new Shared.Messages.RequestHandler { Name = s.Name, QueueName = s.QueueName, RequestType = s.RequestType.FullName, ResponseType = s.ResponseType.FullName });

                _messageBroker.SendMessageBusHandshake(subscribeHandlers, requestHandlers, _cancellationTokenSource.Token);

                _reconnectCount = 0;
            }
            catch (Exception exception)
            {
                if (++_reconnectCount > 20)
                {
                    _logger.Critical(exception, "Failed to give handshake to Message Broker, stopping Message Bus");

                    Stop();

                    throw;
                }

                _logger.Error(exception, "Failed to give handshake to Message Broker, reconnecting in a bit {ReconnectCount}", _reconnectCount);
            }
        }

        /// <inheritdoc />
        public void Publish<T>(PublishSubscribeConfig config, T message)
        {
            if (_cancellationTokenSource == null)
                throw new ArgumentException("Cannot Request before Start");

            ValidatePublishSubscribeConfig(config);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _messageBroker.SendPublishMessage(config.Topic, message, config.Persistent, _cancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler)
        {
            SubscribeHandler(config, handler, UniqueKeyUtility.Generate(), false);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name)
        {
            SubscribeHandler(config, handler, name, true);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable)
        {
            SubscribeHandler(config, handler, name, durable, false);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable, bool multiThreaded)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subscribeHandler = CreateSubscribeHandler(config, name, durable).Set(typeof(T), multiThreaded, (m, c) => handler((T)m, c));

            if (_cancellationTokenSource != null)
                StartSubscribeHandler(subscribeHandler);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler)
        {
            SubscribeHandler(config, handler, UniqueKeyUtility.Generate(), false);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name)
        {
            SubscribeHandler(config, handler, name, true);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name, bool durable)
        {
            SubscribeHandler(config, handler, name, durable, false);
        }

        /// <inheritdoc />
        public void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name, bool durable, bool multiThreaded)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subscribeHandler = CreateSubscribeHandler(config, name, durable).Set(typeof(T), multiThreaded, m => handler((T)m));

            if (_cancellationTokenSource != null)
                StartSubscribeHandler(subscribeHandler);
        }

        /// <inheritdoc />
        public TResponse Request<TRequest, TResponse>(RequestResponseConfig config, TRequest request)
        {
            try
            {
                return RequestAsync<TRequest, TResponse>(config, request).Result;
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException != null)
                    throw exception.InnerException;

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TResponse> RequestAsync<TRequest, TResponse>(RequestResponseConfig config, TRequest request)
        {
            if (_cancellationTokenSource == null)
                throw new ArgumentException("Cannot Request before Start");

            ValidateRequestResponseConfig(config);

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await _messageBroker.RequestResponseAsync<TRequest, TResponse>(config.Name, request, config.MillisecondsTimeout, _cancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler)
        {
            RequestHandler(config, handler, false);
        }

        /// <inheritdoc />
        public void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler, bool multiThreaded)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var requestHandler = CreateRequestHandler(config).Set(typeof(TRequest), typeof(TResponse), multiThreaded, (m, c) => handler((TRequest)m, c));

            if (_cancellationTokenSource != null)
                StartRequestHandler(requestHandler);
        }

        /// <inheritdoc />
        public void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, TResponse> handler)
        {
            RequestHandler(config, handler, false);
        }

        /// <inheritdoc />
        public void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, TResponse> handler, bool multiThreaded)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var requestHandler = CreateRequestHandler(config).Set(typeof(TRequest), typeof(TResponse), multiThreaded, m => handler((TRequest)m));

            if (_cancellationTokenSource != null)
                StartRequestHandler(requestHandler);
        }

        private void StartRequestHandler(RequestHandler requestHandler)
        {
            requestHandler.Start(_cancellationTokenSource.Token, SyncMode);

            SendHandshake();
        }

        private void StartSubscribeHandler(SubscribeHandler subscribeHandler)
        {
            subscribeHandler.Start(_cancellationTokenSource.Token, SyncMode);

            SendHandshake();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose locale objects
        /// </summary>
        /// <param name="disposing">Disposing</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    Stop();

                    Parallel.ForEach(_subscribeHandlers, subscribeHandler => subscribeHandler.Dispose());
                    Parallel.ForEach(_requestHandlers, requestHandler => requestHandler.Dispose());

                    _messageBroker.Dispose();
                }
            }
        }

        private SubscribeHandler CreateSubscribeHandler(PublishSubscribeConfig config, string name, bool durable)
        {
            ValidatePublishSubscribeConfig(config);

            if (name.NullOrWhiteSpace())
                throw new ArgumentException("Invalid subscriber name", nameof(name));

            var subscribeHandler = new SubscribeHandler(_logger, _messageBroker, _queueHandlerFactory, name, config.Topic, durable, _queueNameUtility);

            lock (_subscribeHandlers)
            {
                if (_subscribeHandlers.Any(h => h.Name == name && h.Topic == config.Topic))
                    throw new DoubleSubscribeHandlerException(config, name);

                _subscribeHandlers.Add(subscribeHandler);
            }

            return subscribeHandler;
        }

        private RequestHandler CreateRequestHandler(RequestResponseConfig config)
        {
            ValidateRequestResponseConfig(config);

            var requestHandler = new RequestHandler(_logger, _messageBroker, _queueHandlerFactory, config.Name, _queueNameUtility);

            lock (_requestHandlers)
            {
                if (_requestHandlers.Any(h => h.Name == config.Name))
                    throw new DoubleRequestHandlerException(config);

                _requestHandlers.Add(requestHandler);
            }

            return requestHandler;
        }

        private static void ValidatePublishSubscribeConfig(PublishSubscribeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.Topic.NullOrWhiteSpace())
                throw new ArgumentException("Topic must be provided", nameof(config.Topic));
        }

        private static void ValidateRequestResponseConfig(RequestResponseConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.MillisecondsTimeout <= 0)
                throw new ArgumentException("Request timeout must be larger than zero", nameof(config.MillisecondsTimeout));

            if (config.Name.NullOrWhiteSpace())
                throw new ArgumentException("Request name is invalid", nameof(config.Name));
        }
    }
}