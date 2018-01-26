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
    public class RequestHandlerTests
    {
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IMessageBroker _messageBroker;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly CancellationToken _cancellationToken;

        public RequestHandlerTests()
        {
            _messageBusConfig = new MessageBusConfig
            {
                ServiceName = "UnitTest"
            };

            _messageBroker = Substitute.For<IMessageBroker>();
            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
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
                cut.MessageHandler(new RequestMessage { Body = "Message" }, _cancellationToken);
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
                cut.MessageHandler(new RequestMessage { Body = "Message" }, _cancellationToken);
                message.Should().Be("Message");
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidBodyShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(new RequestMessage { Body = 123 }, _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveMessageWithInvalidResponseTypeShouldThrow()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(int), true, a => a);
                Assert.Throws<InvalidMessageTypeException>(() => cut.MessageHandler(new RequestMessage { Body = "Message" }, _cancellationToken));
            }
        }

        [Fact]
        public void ReceiveValidMessageShouldSendRequestCompleteMessage()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                cut.MessageHandler(new RequestMessage { Body = "Message" }, _cancellationToken);
            }
            _messageBroker.Received(1).SendResponseMessage(Arg.Any<string>(), Arg.Any<RequestMessage>(), Arg.Any<object>());
        }

        [Fact]
        public void ErrorHandlerShouldSendResponseErrorMessage()
        {
            using (var cut = CreateRequestHandler())
            {
                cut.Set(typeof(string), typeof(string), true, a => a);
                cut.ErrorHandler(new RequestMessage { Body = "Message" }, new Exception());
            }
            _messageBroker.Received(1).SendResponseErrorMessage(Arg.Any<string>(), Arg.Any<RequestMessage>(), Arg.Any<Exception>());
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
            return new RequestHandler(_messageBusConfig, _messageBroker, _queueHandlerFactory, "MyRequest");
        }
    }
}