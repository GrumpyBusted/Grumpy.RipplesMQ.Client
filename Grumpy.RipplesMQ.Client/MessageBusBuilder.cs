using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client
{
    /// <summary>
    /// Message Bus Builder
    /// </summary>
    public class MessageBusBuilder
    {
        private string _serviceName;

        /// <summary>
        /// Set Service Name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public MessageBusBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;

            return this;
        }

        /// <summary>
        /// Build Message Bus
        /// </summary>
        /// <returns></returns>
        public IMessageBus Build()
        {
            var processInformation = new ProcessInformation();

            var messageBusConfig = new MessageBusConfig
            {
                ServiceName = _serviceName.NullOrEmpty() ? processInformation.ProcessName : _serviceName
            };

            var queueFactory = new QueueFactory();
            var queueNameUtility = new QueueNameUtility(messageBusConfig.ServiceName);
            var messageBroker = new MessageBroker(messageBusConfig, queueFactory, processInformation, queueNameUtility);
            var queueHandlerFactory = new QueueHandlerFactory(queueFactory);

            return new MessageBus(messageBroker, queueHandlerFactory, queueNameUtility);
        }

        /// <summary>
        /// Build Message Bus
        /// </summary>
        /// <param name="messageBusBuilder"></param>
        /// <returns>The Message Bus</returns>
        public static implicit operator MessageBus(MessageBusBuilder messageBusBuilder)
        {
            return (MessageBus)messageBusBuilder.Build();
        }
    }
}