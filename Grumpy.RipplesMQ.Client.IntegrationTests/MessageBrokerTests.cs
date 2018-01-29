using Grumpy.Common;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Shared.Config;
using Xunit;

namespace Grumpy.RipplesMQ.Client.IntegrationTests
{
    public class MessageBrokerTests
    {
        [Fact(Skip = "Only work first time, or delete queue from test computer")]
        public void RegisterMessageBusServiceShouldSendMessage()
        {
            var messageBusConfig = new MessageBusConfig { ServiceName = "IntegrationTest" };
            var queueFactory = new QueueFactory();
            var processInformation = new ProcessInformation();
            var queueNameUtility = new QueueNameUtility(messageBusConfig.ServiceName);

            new MessageQueueManager().Delete(MessageBrokerConfig.LocaleQueueName, true);

            Assert.Throws<MessageBrokerException>(() => new MessageBroker(messageBusConfig, queueFactory, processInformation, queueNameUtility));
        }
    }
}
