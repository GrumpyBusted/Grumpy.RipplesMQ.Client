using System;
using System.Threading;
using FluentAssertions;
using Grumpy.Json;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class SubscribeHandlerTests
    {
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly IQueueNameUtility _queueNameUtility;
        private readonly CancellationToken _cancellationToken;

        public SubscribeHandlerTests()
        {
            _messageBroker = Substitute.For<IMessageBroker>();
            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
            _queueNameUtility = Substitute.For<IQueueNameUtility>();
            _cancellationToken = new CancellationToken();
        }

        [Fact]
        public void CanCreateSubscribeHandler()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(typeof(string), false, a => { });
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
                cut.Set(typeof(string), true, a => message = (string)a);
                cut.MessageHandler(CreatePublishMessage("Message"), _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldCallCancellableHandler()
        {
            using (var cut = CreateSubscribeHandler())
            {
                var message = "";
                cut.Set(typeof(string), true, (a,c) => message = (string)a);
                cut.MessageHandler(CreatePublishMessage("Message"), _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidBodyShouldThrow()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(typeof(string), true, a => { });
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(CreatePublishMessage(123), _cancellationToken));
            }
        }
        
        [Fact]
        public void ReceiveValidMessageShouldSendSubscribeCompleteMessage()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(typeof(string), true, a => { });
                cut.MessageHandler(CreatePublishMessage("Message"), _cancellationToken);
            }
            _messageBroker.Received(1).SendSubscribeHandlerCompletedMessage(Arg.Any<string>(), Arg.Any<PublishMessage>());
        }
        
        [Fact]
        public void ErrorHandlerShouldSendSubscribeHandlerErrorMessage()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(typeof(string), true, a => { });
                cut.ErrorHandler(CreatePublishMessage("Message"), new Exception());
            }
            _messageBroker.Received(1).SendSubscribeHandlerErrorMessage(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<PublishMessage>(), Arg.Any<Exception>());
        }

        private static PublishMessage CreatePublishMessage(object message)
        {
            return new PublishMessage
            {
                MessageBody = message.SerializeToJson(),
                MessageType = message.GetType().FullName
            };
        }

        [Fact]
        public void ErrorHandlerWithInvalidMessageShouldThrow()
        {
            using (var cut = CreateSubscribeHandler())
            {
                cut.Set(typeof(string), true, a => { });
                Assert.Throws<InvalidMessageTypeException>(() => cut.ErrorHandler("Message", new Exception()));
            }
        }

        private SubscribeHandler CreateSubscribeHandler()
        {
            return new SubscribeHandler(Substitute.For<ILogger>(), _messageBroker, _queueHandlerFactory, "MySubscriber", "MyTopic", true, _queueNameUtility);
        }
    }
}