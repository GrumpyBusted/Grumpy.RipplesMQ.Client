using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.Common.Threading;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class MessageBus : IMessageBus
    {
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

        /// <inheritdoc />
        public MessageBus(IMessageBroker messageBroker, IQueueHandlerFactory queueHandlerFactory, IQueueNameUtility queueNameUtility)
        {
            _messageBroker = messageBroker;
            _queueHandlerFactory = queueHandlerFactory;
            _queueNameUtility = queueNameUtility;

            _subscribeHandlers = new List<SubscribeHandler>();
            _requestHandlers = new List<RequestHandler>();
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Message Bus not Stopped");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenRegistration = cancellationToken.Register(Stop);

            _messageBroker.CheckServer();

            _messageBroker.RegisterMessageBusService(_cancellationTokenSource.Token);

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
        }

        /// <inheritdoc />
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _handshakeTask?.Dispose();

            Parallel.ForEach(_subscribeHandlers, subscribeHandler => subscribeHandler.Stop());
            Parallel.ForEach(_requestHandlers, requestHandler => requestHandler.Stop());

            _cancellationTokenRegistration.Dispose();
        }

        private void SendHandshake()
        {
            _messageBroker.CheckServer();

            var subscribeHandlers = _subscribeHandlers.Select(s => new Shared.Messages.SubscribeHandler { Name = s.Name, QueueName = s.QueueName, Topic = s.Topic, Durable = s.Durable });
            var requestHandlers = _requestHandlers.Select(s => new Shared.Messages.RequestHandler { Name = s.Name, QueueName = s.QueueName });

            _messageBroker.SendMessageBusHandshake(subscribeHandlers, requestHandlers);
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

            CreateSubscribeHandler(config, name, durable).Set(typeof(T), multiThreaded, (m, c) => handler((T)m, c));
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

            CreateSubscribeHandler(config, name, durable).Set(typeof(T), multiThreaded, m => handler((T)m));
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

            CreateRequestHandler(config).Set(typeof(TRequest), typeof(TResponse), multiThreaded, (m, c) => handler((TRequest)m, c));
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

            CreateRequestHandler(config).Set(typeof(TRequest), typeof(TResponse), multiThreaded, m => handler((TRequest)m));
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
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Cannot add Handler after Start");

            ValidatePublishSubscribeConfig(config);

            if (name.NullOrWhiteSpace())
                throw new ArgumentException("Invalid subscriber name", nameof(name));

            var subscribeHandler = new SubscribeHandler(_messageBroker, _queueHandlerFactory, name, config.Topic, durable, _queueNameUtility);

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
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Cannot add Handler after Start");

            ValidateRequestResponseConfig(config);

            var requestHandler = new RequestHandler(_messageBroker, _queueHandlerFactory, config.Name, _queueNameUtility);

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