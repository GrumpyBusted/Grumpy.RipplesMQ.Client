using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Grumpy.RipplesMQ.Client.TestTools.UnitTests
{
    public class MessageBusBuilderTests
    {
        [Fact]
        public void CanBuildMessageBus()
        {
            var messageBus = new MessageBusBuilder().Build();

            messageBus.GetType().Should().Be<MessageBus>();
        }

        [Fact]
        public void CanBuildMessageBusWithServiceName()
        {
            MessageBus messageBus = new MessageBusBuilder().WithServiceName("MyService");

            messageBus.GetType().Should().Be<MessageBus>();
        }

        [Fact]
        public void CanBuildMessageBusWithLogger()
        {
            MessageBus messageBus = new MessageBusBuilder().WithLogger(NullLogger.Instance);

            messageBus.GetType().Should().Be<MessageBus>();
        }
    }
}