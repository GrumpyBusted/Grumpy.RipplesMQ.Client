using System;
using System.Threading;
using FluentAssertions;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class SubscribeHandlerTests
    {
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly CancellationToken _cancellationToken;

        public SubscribeHandlerTests()
        {
            _messageBusConfig = new MessageBusConfig
            {
                ServiceName = "UnitTest",
                InstanceName = "1"
            };

            _messageBroker = Substitute.For<IMessageBroker>();
            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
            _cancellationToken = new CancellationToken();
        }

        [Fact]
        public void CanCreateSubscribeHandler()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(true, typeof(string), false, a => { });
                cut.Start(_cancellationToken, true);
            }
        }

        [Fact]
        public void CannotStartBeforeSet()
        {
            using (var cut = CreateSubscribeHandler())
            {
                Assert.Throws<ArgumentException>(() => cut.Start(_cancellationToken, true));
            }
        }

        [Fact]
        public void ReceiveStringShouldThrow()
        {
            using (var cut = CreateSubscribeHandler())
            {
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler("Message", _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldCallHandler()
        {
            using (var cut = CreateSubscribeHandler())
            {
                var message = "";
                cut.Set(true, typeof(string), true, a => message = (string)a);
                cut.MessageHandler(new PublishMessage { Body = "Message" }, _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldCallCancellableHandler()
        {
            using (var cut = CreateSubscribeHandler())
            {
                var message = "";
                cut.Set(true, typeof(string), true, (a,c) => message = (string)a);
                cut.MessageHandler(new PublishMessage { Body = "Message" }, _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidBodyShouldThrow()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(true, typeof(string), true, a => { });
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(new PublishMessage { Body = 123 }, _cancellationToken));
            }
        }
        
        [Fact]
        public void ReceiveValidMessageShouldSendSubscribeCompleteMessage()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(true, typeof(string), true, a => { });
                cut.MessageHandler(new PublishMessage { Body = "Message" }, _cancellationToken);
            }
            _messageBroker.Received(1).SendSubscribeHandlerCompletedMessage(Arg.Any<string>(), Arg.Any<PublishMessage>());
        }
        
        [Fact]
        public void ErrorHandlerShouldSendSubscribeHandlerErrorMessage()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(true, typeof(string), true, a => { });
                cut.ErrorHandler(new PublishMessage { Body = "Message" }, new Exception());
            }
            _messageBroker.Received(1).SendSubscribeHandlerErrorMessage(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<PublishMessage>(), Arg.Any<Exception>());
        }

        [Fact]
        public void ErrorHandlerWithInvalidMessageShouldThrow()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(true, typeof(string), true, a => { });
                Assert.Throws<InvalidMessageTypeException>(() => cut.ErrorHandler("Message", new Exception()));
            }
        }

        private SubscribeHandler CreateSubscribeHandler()
        {
            return new SubscribeHandler(_messageBusConfig, _messageBroker, _queueHandlerFactory, "MySubscriber", "MyTopic");
        }
    }
}