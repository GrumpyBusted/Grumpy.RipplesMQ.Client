using System;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Bus for Services
    /// </summary>
    public interface IMessageBus : IDisposable
    {
        /// <summary>
        /// Start the Message Bus
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Stop the Message Bus
        /// </summary>
        void Stop();

        /// <summary>
        /// Publish Message to the Message Bus
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="message">Message to Publish</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void Publish<T>(PublishSubscribeConfig config, T message);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <param name="multiThreaded">Can subscriber handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable, bool multiThreaded);


        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name, bool durable);

        /// <summary>
        /// Register Subscribe Handler
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <param name="multiThreaded">Can subscriber handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        void SubscribeHandler<T>(PublishSubscribeConfig config, Action<T> handler, string name, bool durable, bool multiThreaded);

        /// <summary>
        /// Request a response from a Request Handler
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="request">Request message</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns></returns>
        TResponse Request<TRequest, TResponse>(RequestResponseConfig config, TRequest request);

        /// <summary>
        /// Request a response from a Request Handler Asynchronously
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="request">Request message</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns></returns>
        Task<TResponse> RequestAsync<TRequest, TResponse>(RequestResponseConfig config, TRequest request);

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler);

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <param name="multiThreaded">Can request handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler, bool multiThreaded);

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, TResponse> handler);

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <param name="multiThreaded">Can request handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        void RequestHandler<TRequest, TResponse>(RequestResponseConfig config, Func<TRequest, TResponse> handler, bool multiThreaded);
    }
}