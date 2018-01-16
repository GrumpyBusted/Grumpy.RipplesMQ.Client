﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grumpy.Common.Extensions;
using Grumpy.Common.Interfaces;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Shared.Config;
using Grumpy.RipplesMQ.Shared.Messages;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class MessageBrokerTests
    {
        private readonly MessageBusConfig _messageBusConfig;
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;
        private readonly CancellationToken _cancellationToken;
        private readonly ILocaleQueue _messageBrokerQueue;

        public MessageBrokerTests()
        {
            _messageBusConfig = new MessageBusConfig
            {
                ServiceName = "MessageBrokerTests",
                InstanceName = ""
            };

            _queueFactory = Substitute.For<IQueueFactory>();
            _processInformation = Substitute.For<IProcessInformation>();
            _processInformation.MachineName.Returns("TestServer");

            _cancellationToken = new CancellationToken();

            _messageBrokerQueue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale(MessageBrokerConfig.LocaleQueueName, Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>()).Returns(_messageBrokerQueue);
        }

        [Fact]
        public void RegisterMessageBusServiceShouldSendMessage()
        {
            ReplyQueue<MessageBusServiceRegisterReplyMessage>();

            RegisterMessageBusService();

            _messageBrokerQueue.Received(1).Send(Arg.Any<MessageBusServiceRegisterMessage>());
        }

        [Fact]
        public void RegisterMessageBusServiceWithoutReplyShouldThrowException()
        {
            Assert.Throws<MessageBusServiceRegisterTimeoutException>(() => RegisterMessageBusService());
        }

        [Fact]
        public void RegisterMessageBusServiceShouldReceiveCompletedTime()
        {
            ReplyQueue<MessageBusServiceRegisterReplyMessage>();

            RegisterMessageBusService().CompletedDateTime.Should().NotBeNull();
        }

        [Fact]
        public void RegisterMessageBusServiceShouldExpectReply()
        {
            var replyQueue = ReplyQueue<MessageBusServiceRegisterReplyMessage>();

            RegisterMessageBusService();

            replyQueue.Received(1).Receive<MessageBusServiceRegisterReplyMessage>(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        private MessageBusServiceRegisterReplyMessage RegisterMessageBusService()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                return messageBroker.RegisterMessageBusService(_cancellationToken);
            }
        }

        [Fact]
        public void SendPublishMessageShouldSendToMessageBroker()
        {
            SendPublishMessage(false);

            _messageBrokerQueue.Received(1).Send(Arg.Any<PublishMessage>());
        }

        private PublishReplyMessage SendPublishMessage(bool persistent)
        {
            using (var messageBroker = CreateMessageBroker())
            {
                return messageBroker.SendPublishMessage("MyTopic", "MyMessage", persistent, _cancellationToken);
            }
        }

        [Fact]
        public void SendPublishMessageShouldReceiveCompletedTime()
        {
            SendPublishMessage(false).CompletedDateTime.Should().NotBeNull();
        }

        [Fact]
        public void SendPublishMessageShouldAddMessageId()
        {
            SendPublishMessage(false);

            _messageBrokerQueue.Received(1).Send(Arg.Is<PublishMessage>(m => !m.MessageId.NullOrWhiteSpace()));
        }

        [Fact]
        public void SendPersistentPublishMessageShouldSendPublishMessageWithoutReplyQueue()
        {
            var replyQueue = ReplyQueue<PublishReplyMessage>();

            SendPublishMessage(true);

            replyQueue.Received(1).Receive<PublishReplyMessage>(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void SendPersistentPublishMessageWithoutReplyShouldThrowException()
        {
            Assert.Throws<PublishReplyTimeoutException>(() => SendPublishMessage(true));
        }

        [Fact]
        public void SendNonePersistentPublishMessageShouldSendPublishMessageWithReplyQueue()
        {
            var replyQueue = ReplyQueue<PublishReplyMessage>();

            SendPublishMessage(false);

            replyQueue.Received(0).Receive<PublishReplyMessage>(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterSubscriberShouldSendRegistration()
        {
            ReplyQueue<SubscribeHandlerRegisterReplyMessage>();

            RegisterSubscribeHandler();

            _messageBrokerQueue.Received(1).Send(Arg.Any<SubscribeHandlerRegisterMessage>());
        }

        [Fact]
        public void RegisterSubscriberWithoutReplyShouldThrowException()
        {
            Assert.Throws<SubscribeHandlerRegisterTimeoutException>(() => RegisterSubscribeHandler());
        }

        [Fact]
        public void RegisterSubscriberShouldReceiveCompleteTime()
        {
            ReplyQueue<SubscribeHandlerRegisterReplyMessage>();

            RegisterSubscribeHandler().CompletedDateTime.Should().NotBeNull();
        }

        [Fact]
        public void RegisterSubscriberShouldSendReceiveReply()
        {
            var replyQueue = ReplyQueue<SubscribeHandlerRegisterReplyMessage>();

            RegisterSubscribeHandler();

            replyQueue.Received(1).Receive<SubscribeHandlerRegisterReplyMessage>(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        private SubscribeHandlerRegisterReplyMessage RegisterSubscribeHandler()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                return messageBroker.RegisterSubscribeHandler("MyTopic", "MySubscriber", false, "MyQueue", _cancellationToken);
            }
        }

        [Fact]
        public void SendHandshakeShouldSendMessage()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendMessageBusHandshake(Enumerable.Empty<Shared.Messages.SubscribeHandler>(), Enumerable.Empty<Shared.Messages.RequestHandler>());
            }

            _messageBrokerQueue.Received(1).Send(Arg.Any<MessageBusServiceHandshakeMessage>());
        }

        [Fact]
        public void SendSubscriberCompletedMessageShouldSendCompleteMessageWithMessageType()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendSubscribeHandlerCompletedMessage("MySubscriber", new PublishMessage { Body = "MyMessage" });
            }

            _messageBrokerQueue.Received(1).Send(Arg.Is<SubscribeHandlerCompleteMessage>(m => m.MessageType == typeof(string)));
        }

        [Fact]
        public void SendSubscriberErrorMessageShouldSendCompleteMessageWithMessageType()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendSubscribeHandlerErrorMessage("MySubscriber", false, new PublishMessage { Topic = "MyTopic", Body = "MyMessage" }, new Exception("MyException"));
            }

            _messageBrokerQueue.Received(1).Send(Arg.Is<SubscribeHandlerErrorMessage>(m => m.Message.Body is string));
        }

        [Fact]
        public void RequestResponseShouldSendToMessageBroker()
        {
            ReplyQueue(new ResponseMessage { Body = "MyResponse" });
            
            RequestResponse();

            _messageBrokerQueue.Received(1).Send(Arg.Any<RequestMessage>());
        }

        [Fact]
        public void RequestResponseShouldReturnResponse()
        {
            ReplyQueue(new ResponseMessage { Body = "MyResponse" });
            
            RequestResponse().Should().Be("MyResponse");
        }

        [Fact]
        public void RequestResponseWithoutReplyShouldThrowException()
        {
            Assert.Throws<AggregateException>(() => RequestResponse());
        }

        [Fact]
        public void RequestResponseShouldReceiveReply()
        {
            var queue = ReplyQueue(new ResponseMessage { Body = "MyResponse" });

            RequestResponse();

            queue.Received(1).ReceiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RequestResponseWithInvalidTypeShouldThrowException()
        {
            ReplyQueue(new ResponseMessage { Body = 1 });

            Assert.Throws<AggregateException>(() => RequestResponse());
        }

        [Fact]
        public void ReceiveErrorResponseMessageShouldThrowException()
        {
            ReplyQueue(new ResponseErrorMessage { RequestMessage = new RequestMessage(), Exception = new Exception() });

            Assert.Throws<AggregateException>(() => RequestResponse());
        }

        [Fact]
        public void ReceiveErrorResponseMessageShouldReceiveReply()
        {
            var queue = ReplyQueue(new ResponseErrorMessage { RequestMessage = new RequestMessage(), Exception = new Exception() });

            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.RequestResponseAsync<string, string>("MyRequester", "MyRequest", 1000, _cancellationToken);
            }

            queue.Received(1).ReceiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        private string RequestResponse()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                return messageBroker.RequestResponseAsync<string, string>("MyRequester", "MyRequest", 1000, _cancellationToken)?.Result;
            }
        }

        [Fact]
        public void RegisterRequestHandlerShouldSendRegistration()
        {
            ReplyQueue<RequestHandlerRegisterReplyMessage>();

            RegisterRequestHandler();

            _messageBrokerQueue.Received(1).Send(Arg.Any<RequestHandlerRegisterMessage>());
        }

        [Fact]
        public void RegisterRequestHandlerWithoutReplyShouldThrowException()
        {
            Assert.Throws<RequestHandlerRegisterTimeoutException>(() => RegisterRequestHandler());
        }

        [Fact]
        public void RegisterRequestHandlerShouldReceiveCompleteTime()
        {
            ReplyQueue<RequestHandlerRegisterReplyMessage>();

            RegisterRequestHandler().CompletedDateTime.Should().NotBeNull();
        }

        [Fact]
        public void RegisterRequestHandlerShouldReceiveReply()
        {
            var replyQueue = ReplyQueue<RequestHandlerRegisterReplyMessage>();

            RegisterRequestHandler();

            replyQueue.Received(1).Receive<RequestHandlerRegisterReplyMessage>(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        private RequestHandlerRegisterReplyMessage RegisterRequestHandler()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                return messageBroker.RegisterRequestHandler("MyQueue", "MyRequest", _cancellationToken);
            }
        }

        [Fact]
        public void SendResponseMessageShouldSendToMessageBroker()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendResponseMessage("MyRequester", new RequestMessage { RequesterServerName = "AnotherServer" }, "MyResponse");
            }

            _messageBrokerQueue.Received(1).Send(Arg.Any<ResponseMessage>());
        }

        [Fact]
        public void SendResponseMessageToLocaleRequesterShouldSendToReplyQueue()
        {
            var replyQueue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale("RequestMyReplyQueue", Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>()).Returns(replyQueue);

            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendResponseMessage("RequestMyReplyQueue", new RequestMessage { RequesterServerName = "TestServer" }, "MyResponse");
            }

            replyQueue.Received(1).Send(Arg.Any<ResponseMessage>());
        }

        [Fact]
        public void SendResponseErrorMessageShouldSendToMessageBroker()
        {
            using (var messageBroker = CreateMessageBroker())
            {
                messageBroker.SendResponseErrorMessage("MyRequester", new RequestMessage(), new Exception("Exception"));
            }

            _messageBrokerQueue.Received(1).Send(Arg.Any<ResponseErrorMessage>());
        }

        private IMessageBroker CreateMessageBroker()
        {
            return new MessageBroker(_messageBusConfig, _queueFactory, _processInformation);
        }

        private ILocaleQueue ReplyQueue<T>() where T : new()
        {
            return ReplyQueue(new T());
        }

        private ILocaleQueue ReplyQueue<T>(T reply)
        {
            var transactionalMessage = Substitute.For<ITransactionalMessage>();

            transactionalMessage.Message.Returns(reply);
            transactionalMessage.Type.Returns(reply.GetType());
            transactionalMessage.Body.Returns(reply.SerializeToJson());

            var replyQueue = Substitute.For<ILocaleQueue>();

            replyQueue.Receive<T>(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(reply);
            replyQueue.Receive(Arg.Any<int>(), _cancellationToken).Returns(transactionalMessage);
            replyQueue.ReceiveAsync(Arg.Any<int>(), _cancellationToken).Returns(Task.FromResult(transactionalMessage));

            _queueFactory.CreateLocale(Arg.Is<string>(n => n.Contains(".Reply.")), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>()).Returns(replyQueue);

            return replyQueue;
        }
    }
}