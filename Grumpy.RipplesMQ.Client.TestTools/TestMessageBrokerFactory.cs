using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    /// <inheritdoc />
    public class TestMessageBrokerFactory : IMessageBrokerFactory
    {
        private readonly IMessageBroker _messageBroker;

        /// <inheritdoc />
        public TestMessageBrokerFactory(IMessageBroker testMessageBroker)
        {
            _messageBroker = testMessageBroker;
        }

        /// <inheritdoc />
        public IMessageBroker Create(MessageBusConfig messageBusConfig)
        {
            return _messageBroker;
        }
    }
}