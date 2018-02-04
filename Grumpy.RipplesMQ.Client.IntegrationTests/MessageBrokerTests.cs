using Grumpy.Common;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Shared.Config;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.IntegrationTests
{
    public class MessageBrokerTests
    {
        [Fact(Skip = "Only work first time, or delete queue from test computer")]
        public void RegisterMessageBusServiceShouldSendMessage()
        {
            var messageBusConfig = new MessageBusConfig { ServiceName = "IntegrationTest" };
            var queueFactory = new QueueFactory(Substitute.For<ILogger>());
            var processInformation = new ProcessInformation();
            var queueNameUtility = new QueueNameUtility(messageBusConfig.ServiceName);

            new MessageQueueManager(Substitute.For<ILogger>()).Delete(MessageBrokerConfig.LocaleQueueName, true);

            Assert.Throws<MessageBrokerException>(() => new MessageBroker(Substitute.For<ILogger>(), messageBusConfig, queueFactory, processInformation, queueNameUtility));
        }
    }
}
