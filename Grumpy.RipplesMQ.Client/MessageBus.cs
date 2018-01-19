using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.Common.Threading;
using Grumpy.Json;
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
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly List<SubscribeHandler> _subscribeHandlers;
        private readonly List<RequestHandler> _requestHandlers;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private bool _disposed;
        private TimerTask _handshakeTask;

        /// <inheritdoc />
        public MessageBus(MessageBusConfig messageBusConfig, IMessageBrokerFactory messageBrokerFactory, IQueueHandlerFactory queueHandlerFactory)
        {
            _messageBusConfig = messageBusConfig;
            _messageBroker = messageBrokerFactory.Create(messageBusConfig);
            _queueHandlerFactory = queueHandlerFactory;

            _subscribeHandlers = new List<SubscribeHandler>();
            _requestHandlers = new List<RequestHandler>();
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken, bool syncMode)
        {
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Cannot Start Twice");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenRegistration = cancellationToken.Register(Stop);

            _messageBroker.RegisterMessageBusService(_cancellationTokenSource.Token).SerializeToJson();

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var subscribeHandler in _subscribeHandlers)
            {
                subscribeHandler.Start(_cancellationTokenSource.Token, syncMode);
            }

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var requestHandler in _requestHandlers)
            {
                requestHandler.Start(_cancellationTokenSource.Token, syncMode);
            }

            _handshakeTask = new TimerTask();
            _handshakeTask.Start(SendHandshake, 30000, _cancellationTokenSource.Token);
        }

        private void SendHandshake()
        {
            var subscribeHandlers = _subscribeHandlers.Select(s => new Shared.Messages.SubscribeHandler { Name = s.Name, QueueName = s.QueueName, Topic = s.Topic, Durable = s.Durable });
            var requestHandlers = _requestHandlers.Select(s => new Shared.Messages.RequestHandler { Name = s.Name, QueueName = s.QueueName });

            _messageBroker.SendMessageBusHandshake(subscribeHandlers, requestHandlers);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
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

            CreateSubscribeHandler(config, name).Set(durable, typeof(T), multiThreaded, (m, c) => handler((T)m, c));
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

            CreateSubscribeHandler(config, name).Set(durable, typeof(T), multiThreaded, m => handler((T)m));
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
                if (disposing)
                {
                    Stop();

                    _handshakeTask?.Dispose();

                    Parallel.ForEach(_subscribeHandlers, subscribeHandler => subscribeHandler.Dispose());
                    Parallel.ForEach(_requestHandlers, requestHandler => requestHandler.Dispose());

                    _messageBroker.Dispose();

                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenRegistration.Dispose();
                }

                _disposed = true;
            }
        }

        private SubscribeHandler CreateSubscribeHandler(PublishSubscribeConfig config, string name)
        {
            if (_cancellationTokenSource != null)
                throw new ArgumentException("Cannot add Handler after Start");

            ValidatePublishSubscribeConfig(config);

            if (name.NullOrWhiteSpace())
                throw new ArgumentException("Invalid subscriber name", nameof(name));

            var subscribeHandler = new SubscribeHandler(_messageBusConfig, _messageBroker, _queueHandlerFactory, name, config.Topic);

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

            var requestHandler = new RequestHandler(_messageBusConfig, _messageBroker, _queueHandlerFactory, config.Name);

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