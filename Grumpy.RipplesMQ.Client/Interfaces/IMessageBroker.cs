using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Broker Client
    /// </summary>
    public interface IMessageBroker : IDisposable
    {
        /// <summary>
        /// Register a Message bus Service in the Message Broker
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        MessageBusServiceRegisterReplyMessage RegisterMessageBusService(CancellationToken cancellationToken);

        /// <summary>
        /// Register Subscribe Handler with the Message Broker
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        /// <param name="topic">Topic/Subject</param>
        /// <param name="name">Subscriber Name</param>
        /// <param name="durable">Is Subscriber Durable</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        SubscribeHandlerRegisterReplyMessage RegisterSubscribeHandler(string name, string topic, bool durable, string queueName, CancellationToken cancellationToken);

        /// <summary>
        /// Register Request Handler with the Message Broker
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="name">Request Name</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        RequestHandlerRegisterReplyMessage RegisterRequestHandler(string name, string queueName, CancellationToken cancellationToken);

        /// <summary>
        /// Send Message Bus Handshake
        /// </summary>
        /// <param name="subscribeHandlers">Subscribe handlers</param>
        /// <param name="requestHandlers">Request handlers</param>
        void SendMessageBusHandshake(IEnumerable<Shared.Messages.SubscribeHandler> subscribeHandlers, IEnumerable<Shared.Messages.RequestHandler> requestHandlers);

        /// <summary>
        /// Send a Publish Message to the Message Broker
        /// </summary>
        /// <param name="topic">Topic/Subject</param>
        /// <param name="message">Message to Publish</param>
        /// <param name="persistent">Is Message Persistent</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <typeparam name="T">Type of Message</typeparam>
        /// <returns>Publish Reply Message</returns>
        PublishReplyMessage SendPublishMessage<T>(string topic, T message, bool persistent, CancellationToken cancellationToken);

        /// <summary>
        /// Send Subscribe Handler Completed Message to the Message Broker
        /// </summary>
        /// <param name="name">Subscriber Name</param>
        /// <param name="publishMessage">Publish Message</param>
        void SendSubscribeHandlerCompletedMessage(string name, PublishMessage publishMessage);

        /// <summary>
        /// Send Subscribe Handler Error Message to the Message Broker
        /// </summary>
        /// <param name="name">Subscriber Name</param>
        /// <param name="durable">Durable Subscriber</param>
        /// <param name="publishMessage">Publish Message</param>
        /// <param name="exception">Exception</param>
        void SendSubscribeHandlerErrorMessage(string name, bool durable, PublishMessage publishMessage, Exception exception);

        /// <summary>
        /// Send Request Message to the Message Broker
        /// </summary>
        /// <param name="name">Request Name</param>
        /// <param name="request">Request Message</param>
        /// <param name="millisecondsTimeout">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <typeparam name="TRequest">Type of Request Message</typeparam>
        /// <typeparam name="TResponse">Type of Response Message</typeparam>
        /// <returns>Response Message</returns>
        Task<TResponse> RequestResponseAsync<TRequest, TResponse>(string name, TRequest request, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Send Response Message to the Message Broker
        /// </summary>
        /// <param name="replyQueue">Reply Queue Name</param>
        /// <param name="requestMessage">Request Message</param>
        /// <param name="response">Response Message</param>
        void SendResponseMessage(string replyQueue, RequestMessage requestMessage, object response);

        /// <summary>
        /// Send error Response Message to Message Broker
        /// </summary>
        /// <param name="replyQueue">Reply Queue</param>
        /// <param name="requestMessage">Request Message</param>
        /// <param name="exception">Exception</param>
        void SendResponseErrorMessage(string replyQueue, RequestMessage requestMessage, Exception exception);
    }
}