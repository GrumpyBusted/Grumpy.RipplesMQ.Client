using Grumpy.Common;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client
{
    /// <summary>
    /// Builder for creating Message Bus
    /// </summary>
    public static class MessageBusBuilder
    {
        /// <summary>
        /// Build Message Bus
        /// </summary>
        /// <param name="serviceName">Service Name</param>
        /// <param name="instanceName">Instance Name</param>
        /// <returns></returns>
        public static IMessageBus Build(string serviceName, string instanceName = "")
        {
            var messageBusConfig = new MessageBusConfig
            {
                ServiceName = serviceName,
                InstanceName = instanceName
            };

            var queueFactory = new QueueFactory();

            return new MessageBus(messageBusConfig, new MessageBrokerFactory(queueFactory, new ProcessInformation()), new QueueHandlerFactory(queueFactory));
        }
    }
}