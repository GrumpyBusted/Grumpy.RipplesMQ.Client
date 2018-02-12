using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Config;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class MessageBusTests
    {
        private readonly IMessageBroker _messageBroker;
        private readonly CancellationToken _cancellationToken;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly IQueueHandler _queueHandler;
        private readonly IQueueNameUtility _queueNameUtility;

        public MessageBusTests()
        {
            _messageBroker = Substitute.For<IMessageBroker>();
            _cancellationToken = new CancellationToken();
            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
            _queueHandler = Substitute.For<IQueueHandler>();
            _queueNameUtility = Substitute.For<IQueueNameUtility>();
            _queueHandlerFactory.Create().Returns(_queueHandler);
        }

        [Fact]
        public void StartMessageBusShouldSendHandshake()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(2).SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void StartTwiceMessageBusShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentException>(() => cut.Start(_cancellationToken));
            }
        }

        [Fact]
        public void StopMessageBusShouldWork()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Stop();
            }
        }

        [Fact]
        public void PublishMessageShouldSendMessage()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                cut.Publish(new PublishSubscribeConfig { Persistent = true, Topic = "MyTopic" }, "MyMessage");
            }

            _messageBroker.Received(1).SendPublishMessage("MyTopic", "MyMessage", Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void PublishMessageBeforeStartShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentException>(() => cut.Publish(new PublishSubscribeConfig { Persistent = true, Topic = "MyTopic" }, "MyMessage"));
            }
        }

        [Fact]
        public void PublishMessageWithNullMessageShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentNullException>(() => cut.Publish<string>(new PublishSubscribeConfig { Persistent = true, Topic = "MyTopic" }, null));
            }
        }

        [Fact]
        public void PublishMessageWithNullConfigShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentNullException>(() => cut.Publish(null, "MyMessage"));
            }
        }

        [Fact]
        public void PublishMessageWithInvalidTopicShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentException>(() => cut.Publish(new PublishSubscribeConfig { Persistent = true, Topic = "" }, "MyMessage"));
            }
        }

        [Fact]
        public void RegisterSubscriberWithCancellableHandlerShouldRegisterWithMessageBroker()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (m, c) => { }, "MySubscriber", true, true);
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Is<IEnumerable<Shared.Messages.SubscribeHandler>>(l => l.Count(s => s.Name == "MySubscriber") == 1), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterSubscriberWithNoneCancellableHandlerShouldRegisterWithMessageBroker()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, m => { }, "MySubscriber", true, true);
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Is<IEnumerable<Shared.Messages.SubscribeHandler>>(l => l.Count(s => s.Name == "MySubscriber") == 1), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterSubscriberWithNullCancellableHandlerShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (Action<object, CancellationToken>)null, "MySubscriber", true));
            }
        }

        [Fact]
        public void RegisterSubscriberWithNullNoneCancellableHandlerShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (Action<object>)null, "MySubscriber", true));
            }
        }

        [Fact]
        public void RegisterSubscriberWithInvalidNameShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentException>(() => cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (m, c) => { }, ""));
            }
        }

        [Fact]
        public void RegisterCancellableSubscriberWithNameShouldGetDurableQueue()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (m, c) => { }, "MySubscriber");
                cut.Start(_cancellationToken);
            }

            _queueHandler.Received(1).Start(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<Action<object, CancellationToken>>(), Arg.Any<Action<object, Exception>>(), Arg.Any<Action>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterSubscriberHandlerAfterStartShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentException>(() => cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, (m, c) => { }, "MySubscriber"));
            }
        }

        [Fact]
        public void RegisterNoneCancellableSubscriberWithNameShouldGetDurableQueue()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, m => { }, "MySubscriber");
                cut.Start(_cancellationToken);
            }

            _queueHandler.Received(1).Start(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<Action<object, CancellationToken>>(), Arg.Any<Action<object, Exception>>(), Arg.Any<Action>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterSubscriberWithoutNameShouldGetNoneDurableQueue()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, m => { });
                cut.Start(_cancellationToken);
            }

            _queueHandler.Received(1).Start(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<Action<object, CancellationToken>>(), Arg.Any<Action<object, Exception>>(), Arg.Any<Action>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void DoubleRequestSubscriberNotAllowed()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, m => { }, "MySubscriber");
                Assert.Throws<DoubleSubscribeHandlerException>(() => cut.SubscribeHandler<string>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopic" }, m => { }, "MySubscriber"));
            }
        }

        [Fact]
        public void RegisterRequestNoneCancellableHandlerShouldRegisterWithMessageBroker()
        {
            using (var cut = CreateMessageBus())
            {
                cut.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, s => s);
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Is<IEnumerable<Shared.Messages.RequestHandler>>(l => l.Count(s => s.Name == "MyRequester") == 1), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterRequestCancellableHandlerWithNullHandlerShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, (s, c) => s);
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Is<IEnumerable<Shared.Messages.RequestHandler>>(l => l.Count(s => s.Name == "MyRequester") == 1), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterRequestNoneCancellableHandlerWithNullHandlerShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => cut.RequestHandler(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, (Func<string, CancellationToken, string>)null, true));
            }
        }

        [Fact]
        public void RegisterRequestCancellableHandlerShouldRegisterWithMessageBroker()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => cut.RequestHandler(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, (Func<string, string>)null));
            }
        }

        [Fact]
        public void RegisterRequestHandlerShouldStartQueueHandler()
        {
            using (var cut = CreateMessageBus())
            {
                cut.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, s => s);
                cut.Start(_cancellationToken);
            }

            _queueHandler.Received(1).Start(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<Action<object, CancellationToken>>(), Arg.Any<Action<object, Exception>>(), Arg.Any<Action>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void AddRequestHandlerWithFluentSyntaxShouldWork()
        {
            using (var messageBus = CreateMessageBus())
            {
                messageBus
                    .AddRequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequesterA", MillisecondsTimeout = 100 }, (s, c) => s)
                    .AddRequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequesterB", MillisecondsTimeout = 100 }, (s, c) => s, false)
                    .AddRequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequesterC", MillisecondsTimeout = 100 }, s => s)
                    .AddRequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequesterD", MillisecondsTimeout = 100 }, s => s, false)
                    .Start(new CancellationToken());
            }
        }

        [Fact]
        public void AddSubscriberHandlerWithFluentSyntaxShouldWork()
        {
            using (var messageBus = CreateMessageBus())
            {
                messageBus
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicA", Persistent = false }, (s, c) => { })
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicB", Persistent = false }, (s, c) => { }, "MySubscriber")
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicC", Persistent = false }, (s, c) => { }, "MySubscriber", true)
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicD", Persistent = false }, (s, c) => { }, "MySubscriber", true, true)
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicE", Persistent = false }, s => { })
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicF", Persistent = false }, s => { }, "MySubscriber")
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicG", Persistent = false }, s => { }, "MySubscriber", true)
                    .AddSubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopicH", Persistent = false }, s => { }, "MySubscriber", true, true)
                    .Start(new CancellationToken());
            }
        }

        [Fact]
        public void DoubleRequestHandlerNotAllowed()
        {
            using (var messageBus = CreateMessageBus())
            {
                messageBus.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, s => s, false);
                Assert.Throws<DoubleRequestHandlerException>(() => messageBus.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, s => s, false));
            }
        }

        [Fact]
        public void RequestHandlerWithNullRequestShouldThrowException()
        {
            using (var messageBus = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => messageBus.RequestHandler(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, (Func<string, string>)null));
            }
        }

        [Fact]
        public void RequestHandlerAfterStartShouldThrowException()
        {
            using (var messageBus = CreateMessageBus())
            {
                messageBus.Start(_cancellationToken);
                Assert.Throws<ArgumentException>(() => messageBus.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, s => s));
            }
        }

        [Fact]
        public void RequestHandlerWithNullConfigShouldThrowException()
        {
            using (var messageBus = CreateMessageBus())
            {
                Assert.Throws<ArgumentNullException>(() => messageBus.RequestHandler<string, string>(null, s => s));
            }
        }

        [Fact]
        public void RequestHandlerWitZeroTimeoutShouldThrowException()
        {
            using (var messageBus = CreateMessageBus())
            {
                Assert.Throws<ArgumentException>(() => messageBus.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 0 }, s => s));
            }
        }

        [Fact]
        public void RequestHandlerWitEmptyNameShouldThrowException()
        {
            using (var messageBus = CreateMessageBus())
            {
                Assert.Throws<ArgumentException>(() => messageBus.RequestHandler<string, string>(new RequestResponseConfig { Name = "", MillisecondsTimeout = 1000 }, s => s));
            }
        }

        [Fact]
        public void RequestBeforeStartShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                Assert.Throws<ArgumentException>(() => cut.Request<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, null));
            }
        }

        [Fact]
        public void RequestWithNullRequestShouldThrowException()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                Assert.Throws<ArgumentNullException>(() => cut.Request<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, null));
            }
        }

        [Fact]
        public void RequestShouldSendRequestToMessageBroker()
        {
            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                cut.Request<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, "MyRequest");
            }

            _messageBroker.Received(1).RequestResponseAsync<string, string>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RequestShouldReturnResponse()
        {
            _messageBroker.RequestResponseAsync<string, string>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("MyResponse"));

            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                var response = cut.Request<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, "MyRequest");

                response.Should().Be("MyResponse");
            }
        }

        [Fact]
        public void RequestAsyncShouldReturnResponse()
        {
            _messageBroker.RequestResponseAsync<string, string>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("MyResponse"));

            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
                var response = cut.RequestAsync<string, string>(new RequestResponseConfig { Name = "MyRequester", MillisecondsTimeout = 100 }, "MyRequest").Result;

                response.Should().Be("MyResponse");
            }
        }

        [Fact]
        public void SendHandshakeShouldIncludeSubscribeHandlers()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopic" }, s => { });
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Is<IEnumerable<Shared.Messages.SubscribeHandler>>(s => s.Count(h => h.Topic == "MyTopic") == 1), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void SendHandshakeShouldIncludeRequestHandlers()
        {
            using (var cut = CreateMessageBus())
            {
                cut.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequestHandler", MillisecondsTimeout = 100 }, s => s);
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Is<IEnumerable<Shared.Messages.RequestHandler>>(r => r.Count(h => h.Name == "MyRequestHandler") == 1), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void SendHandshakeFailsShouldRetry()
        {
            _messageBroker.SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>()).Returns(e => throw new Exception());

            using (var cut = CreateMessageBus())
            {
                cut.Start(_cancellationToken);
            }

            _messageBroker.Received(2).SendMessageBusHandshake(Arg.Any<IEnumerable<Shared.Messages.SubscribeHandler>>(), Arg.Any<IEnumerable<Shared.Messages.RequestHandler>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void StopShouldSendEmptyHandshake()
        {
            using (var cut = CreateMessageBus())
            {
                cut.SubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopic" }, s => { });
                cut.RequestHandler<string, string>(new RequestResponseConfig { Name = "MyRequestHandler", MillisecondsTimeout = 100 }, s => s);
                cut.Start(_cancellationToken);
                cut.Stop();
            }

            _messageBroker.Received(1).SendMessageBusHandshake(Arg.Is<IEnumerable<Shared.Messages.SubscribeHandler>>(s => !s.Any()), Arg.Is<IEnumerable<Shared.Messages.RequestHandler>>(r => !r.Any()), Arg.Any<CancellationToken>());
        }

        private IMessageBus CreateMessageBus()
        {
            return new MessageBus(Substitute.For<ILogger>(), _messageBroker, _queueHandlerFactory, _queueNameUtility) { SyncMode = true };
        }
    }
}