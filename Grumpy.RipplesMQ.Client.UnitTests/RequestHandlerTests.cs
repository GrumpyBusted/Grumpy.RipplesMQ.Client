using System;
using System.Threading;
using FluentAssertions;
using Grumpy.Json;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class RequestHandlerTests
    {
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly IQueueNameUtility _queueNameUtility;
        private readonly CancellationToken _cancellationToken;

        public RequestHandlerTests()
        {
            _messageBroker = Substitute.For<IMessageBroker>();
            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
            _queueNameUtility = Substitute.For<IQueueNameUtility>();
            _cancellationToken = new CancellationToken();
        }

        [Fact]
        public void CanCreateRequestHandler()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), false, a => a);
                cut.Start(_cancellationToken, true);
            }
        }

        [Fact]
        public void CannotStartBeforeSet()
        {
            using (var cut = CreateRequestHandler())
            {
                Assert.Throws<ArgumentException>(() => cut.Start(_cancellationToken, true));
            }
        }

        [Fact]
        public void RequestStringShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler("Message", _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldCallHandler()
        {
            using (var cut = CreateRequestHandler())
            {
                var message = "";
                cut.Set(typeof(string), typeof(string), true, a =>
                {
                    message = (string)a;
                    return a;
                });
                cut.MessageHandler(CreateRequestMessage("Message"), _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldCallCancellableHandler()
        {
            using (var cut = CreateRequestHandler())
            {
                var message = "";
                cut.Set(typeof(string), typeof(string), true, (a, c) =>
                {
                    message = (string)a;
                    return a;
                });
                cut.MessageHandler(CreateRequestMessage("Message"), _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidBodyShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(int), typeof(string), true, a => a);
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(CreateRequestMessage("Message"), _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidResponseTypeShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(int), true, a => a);
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(CreateRequestMessage("Message"), _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldSendRequestCompleteMessage()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                cut.MessageHandler(CreateRequestMessage("Message"), _cancellationToken);
            }
            _messageBroker.Received(1).SendResponseMessage(Arg.Any<string>(), Arg.Any<RequestMessage>(), Arg.Any<object>());
        }

        [Fact]
        public void ErrorHandlerShouldSendResponseErrorMessage()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                cut.ErrorHandler(CreateRequestMessage("Message"), new Exception());
            }
            _messageBroker.Received(1).SendResponseErrorMessage(Arg.Any<string>(), Arg.Any<RequestMessage>(), Arg.Any<Exception>());
        }

        private static RequestMessage CreateRequestMessage(object message)
        {
            return new RequestMessage
            {
                MessageBody = message.SerializeToJson(),
                MessageType = message.GetType().FullName
            };
        }

        [Fact]
        public void ErrorHandlerWithInvalidMessageShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                Assert.Throws<InvalidMessageTypeException>(() => cut.ErrorHandler("Message", new Exception()));
            }
        }

        private RequestHandler CreateRequestHandler()
        {
            return new RequestHandler(_messageBroker, _queueHandlerFactory, "MyRequest", _queueNameUtility);
        }
    }
}