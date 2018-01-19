using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    public class MessageBrokerFactory : IMessageBrokerFactory
    {
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;

        /// <inheritdoc />
        public MessageBrokerFactory(IQueueFactory queueFactory, IProcessInformation processInformation)
        {
            _queueFactory = queueFactory;
            _processInformation = processInformation;
        }
        /// <inheritdoc />
        public IMessageBroker Create(MessageBusConfig messageBusConfig)
        {
            return new MessageBroker(messageBusConfig, _queueFactory, _processInformation);
        }
    }
}