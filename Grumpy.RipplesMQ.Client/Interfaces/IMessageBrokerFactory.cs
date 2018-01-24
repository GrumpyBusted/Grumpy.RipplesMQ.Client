namespace Grumpy.RipplesMQ.Client.Interfaces
{
    /// <summary>
    /// Message Broker Factory
    /// </summary>
    public interface IMessageBrokerFactory
    {
        /// <summary>
        /// Create Message Broker
        /// </summary>
        /// <param name="messageBusConfig">Message Bus configuration</param>
        /// <returns></returns>
        IMessageBroker Create(MessageBusConfig messageBusConfig);
    }
}