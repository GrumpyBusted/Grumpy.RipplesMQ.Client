using System;
using System.Threading;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client
{
    /// <summary>
    /// Extension methods for Message Bus to enable fluent syntax for adding handlers
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T, CancellationToken> handler)
        {
            messageBus.SubscribeHandler(config, handler);

            return messageBus;
        }

        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name)
        {
            messageBus.SubscribeHandler(config, handler, name);

            return messageBus;
        }

        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable)
        {
            messageBus.SubscribeHandler(config, handler, name, durable);

            return messageBus;
        }

        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <param name="multiThreaded">Can subscriber handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T, CancellationToken> handler, string name, bool durable, bool multiThreaded)
        {
            messageBus.SubscribeHandler(config, handler, name, durable, multiThreaded);

            return messageBus;
        }



        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T> handler)
        {
            messageBus.SubscribeHandler(config, handler);

            return messageBus;
        }


        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T> handler, string name)
        {
            messageBus.SubscribeHandler(config, handler, name);

            return messageBus;
        }


        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T> handler, string name, bool durable)
        {
            messageBus.SubscribeHandler(config, handler, name, durable);

            return messageBus;
        }


        /// <summary>
        /// Add Subscribe Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="handler">Subscribe handler</param>
        /// <param name="name">Subscriber name</param>
        /// <param name="durable">Should subscriber be durable</param>
        /// <param name="multiThreaded">Can subscriber handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="T">Type of Message Object</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddSubscribeHandler<T>(this IMessageBus messageBus, PublishSubscribeConfig config, Action<T> handler, string name, bool durable, bool multiThreaded)
        {
            messageBus.SubscribeHandler(config, handler, name, durable, multiThreaded);

            return messageBus;
        }

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddRequestHandler<TRequest, TResponse>(this IMessageBus messageBus, RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler)
        {
            messageBus.RequestHandler(config, handler);

            return messageBus;
        }

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <param name="multiThreaded">Can request handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddRequestHandler<TRequest, TResponse>(this IMessageBus messageBus, RequestResponseConfig config, Func<TRequest, CancellationToken, TResponse> handler, bool multiThreaded)
        {
            messageBus.RequestHandler(config, handler, multiThreaded);

            return messageBus;
        }

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddRequestHandler<TRequest, TResponse>(this IMessageBus messageBus, RequestResponseConfig config, Func<TRequest, TResponse> handler)
        {
            messageBus.RequestHandler(config, handler);

            return messageBus;
        }

        /// <summary>
        /// Register Request Handler
        /// </summary>
        /// <param name="messageBus">The Message Bus</param>
        /// <param name="config">Request/Response Configuration</param>
        /// <param name="handler">Request handler</param>
        /// <param name="multiThreaded">Can request handler, handle multi threaded messages, multiple messages in parallel</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response massage</typeparam>
        /// <returns>The Message Bus</returns>
        public static IMessageBus AddRequestHandler<TRequest, TResponse>(this IMessageBus messageBus, RequestResponseConfig config, Func<TRequest, TResponse> handler, bool multiThreaded)
        {
            messageBus.RequestHandler(config, handler, multiThreaded);

            return messageBus;
        }
    }
}
