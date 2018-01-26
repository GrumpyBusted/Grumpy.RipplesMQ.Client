using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Grumpy.RipplesMQ.Client.Exceptions;
using Grumpy.RipplesMQ.Client.Interfaces;
using Grumpy.RipplesMQ.Client.TestTools.UnitTests.Helper;
using Grumpy.RipplesMQ.Config;
using Xunit;

namespace Grumpy.RipplesMQ.Client.TestTools.UnitTests
{
    public class TestMessageBusTests : MessageBusServiceTestsBase
    {
        [Fact]
        public void CanStartTestMessageBus()
        {
            Start();
        }
        
        [Fact]
        public void CanRegisterSubscribeHandler()
        {
            MessageBus.SubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopic" }, i => {});

            Start();
        }
        
        [Fact]
        public void CanRegisterRequestHandler()
        {
            MessageBus.RequestHandler<string, string>(new RequestResponseConfig { MillisecondsTimeout = 1, Name = "MyRequest" }, i => i);

            Start();
        }
        
        [Fact]
        public void CanPublishNonePersistentMessage()
        {
            Start();

            MessageBus.Publish(new PublishSubscribeConfig { Topic = "MyTopic", Persistent = false }, "Message");

            MessageBroker.Published.Should().Contain(m => m.Topic == "MyTopic");
            MessageBroker.Published.Count().Should().Be(1);
        }
        
        [Fact]
        public void CanPublishPersistentMessage()
        {
            Start();

            MessageBus.Publish(new PublishSubscribeConfig { Topic = "MyTopic", Persistent = true }, "Message");

            MessageBroker.Published.Should().Contain(m => m.Topic == "MyTopic");
            MessageBroker.Published.Count().Should().Be(1);
        }
        
        [Fact]
        public void RequestWithoutHandlerShouldThrowTimeout()
        {
            Start();

            Assert.Throws<RequestResponseTimeoutException>(() => MessageBus.Request<string, string>(new RequestResponseConfig { MillisecondsTimeout = 1, Name = "MyRequest" }, "Message"));
        }

        [Fact]
        public void RequestAsyncWithoutHandlerShouldThrowTimeout()
        {
            Start();

            Assert.Throws<AggregateException>(() => MessageBus.RequestAsync<string,string>(new RequestResponseConfig { MillisecondsTimeout = 1, Name = "MyRequest" }, "Message").Result.Should().BeNull());
        }

        [Fact]
        public void RegisterSubscribeHandlerAndPublishOnMessageBusShouldWork()
        {
            var message = "";

            MessageBus.SubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopic" }, i => message = i);

            Start();

            MessageBus.Publish(new PublishSubscribeConfig { Topic = "MyTopic" }, "Message");

            message.Should().Be("Message");
            MessageBroker.Published.Count().Should().Be(1);
        }

        [Fact]
        public void RegisterSubscribeHandlerAndPublishOnMessageBrokerShouldWork()
        {
            var message = "";

            MessageBus.SubscribeHandler<string>(new PublishSubscribeConfig { Topic = "MyTopic" }, i => message = i);

            Start();

            MessageBroker.Publish(new PublishSubscribeConfig { Topic = "MyTopic" }, "Message");

            message.Should().Be("Message");
            MessageBroker.Published.Count().Should().Be(0);
        }

        [Fact]
        public void ServicePublishTwoMessage()
        {
            var service = new MyTestService(MessageBus);

            Start();

            service.DoStuffAndPublishMessage(2);

            MessageBroker.Published.Should().Contain(m => m.Topic == "MyPublishTopic");
            MessageBroker.Published.Count().Should().Be(2);
        }

        [Fact]
        public void RegisterOneSubscriberAndSendOneMessage()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);

            Start();

            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "A" });

            MessageBroker.Published.Count().Should().Be(1);
            MessageBroker.SubscriberHandlerFailed.Count().Should().Be(0);
            MessageBroker.SubscriberHandlerCompleted.Count().Should().Be(1);
        }

        [Fact]
        public void RegisterOneSubscriberAndSendOneMessageThatFailsTheSubscriber()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);

            Start();

            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "Exception" });

            MessageBroker.Published.Count().Should().Be(0);
            MessageBroker.SubscriberHandlerFailed.Count().Should().Be(1);
            MessageBroker.SubscriberHandlerCompleted.Count().Should().Be(0);
        }

        [Fact]
        public void RegisterTwoSubscriberToSameTopicAndSendOneMessage()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);
            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);
            
            Start();

            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "A" });

            MessageBroker.Published.Count().Should().Be(2);
        }

        [Fact]
        public void RegisterOneSubscriberAndSendTwoMessage()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);

            Start();

            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "A" });
            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "A" });

            MessageBroker.Published.Count().Should().Be(2);
        }

        [Fact]
        public void RegisterTwoSubscriberToDifferentTopicAndSendOneMessage()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.SubscribeHandler<MySubscribeDto>(MyTestServiceConfig.MyTestSubscribe, service.MyTestSubscribeHandler);
            MessageBus.SubscribeHandler<MySubscribeDto>(new PublishSubscribeConfig { Topic = "MyTopic" }, service.MyTestSubscribeHandler);

            Start();

            MessageBroker.Publish(MyTestServiceConfig.MyTestSubscribe, new MySubscribeDto { Name = "A" });

            MessageBroker.Published.Count().Should().Be(1);
        }

        [Fact]
        public void ServiceCallRequestGetMockResponse()
        {
            var service = new MyTestService(MessageBus);

            MessageBroker.MockResponse<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, r => r.Name == "YourName", new MyResponseDto { Name = "MyResponse1" });
            MessageBroker.MockResponse<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, r => r.Name == "MyName", new MyResponseDto { Name = "MyResponse2" });
            MessageBroker.MockResponse(MyTestServiceConfig.MyTestRequest, new MyResponseDto { Name = "MyResponse3" });

            Start();

            service.DoStuffAndRequestData();

            service.Name.Should().Be("MyResponse2");
        }

        [Fact]
        public void ServiceCallRequestAsyncGetMockResponse()
        {
            var service = new MyTestService(MessageBus);

            MessageBroker.MockResponse<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, r => r.Name == "YourName", new MyResponseDto { Name = "MyResponse1" });
            MessageBroker.MockResponse<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, r => r.Name == "MyName", new MyResponseDto { Name = "MyResponse2" });
            MessageBroker.MockResponse(MyTestServiceConfig.MyTestRequest, new MyResponseDto { Name = "MyResponse3" });

            Start();

            service.DoStuffAndRequestAsyncData();

            service.Name.Should().Be("MyResponse2");
        }

        [Fact]
        public void RequestResponseFromService()
        {
            var service = new MyTestService(MessageBus);

            MessageBus.RequestHandler<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, service.MyTestRequestHandler);

            Start();

            var response = MessageBroker.Request<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, new MyRequestDto { Name = "MyRequest", Count = 1 });

            response.Name.Should().Be("MyRequest");
            response.Count.Should().Be(1);
        }
    }

    public class MyResponseDtoB
    {
    }

    public class MyRequestDtoB
    {
    }

    public class MyRequestADto
    {
    }

    public class MyResponseADto
    {
    }

    public class MyDtoB
    {
    }

    public class MyResponseBDto
    {
    }

    public class MyRequestBDto
    {
    }

    public class MyRequestDtoA
    {
    }

    public class MyResponseDtoA
    {
    }

    public class MyDtoA
    {
    }
}