[![Build status](https://ci.appveyor.com/api/projects/status/9aiyqm4it2np1x3p?svg=true)](https://ci.appveyor.com/project/GrumpyBusted/grumpy-ripplesmq-client)
[![codecov](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Client/branch/master/graph/badge.svg)](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Client)
[![nuget](https://img.shields.io/nuget/v/Grumpy.RipplesMQ.Client.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Client/)
[![downloads](https://img.shields.io/nuget/dt/Grumpy.RipplesMQ.Client.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Client/)

# Grumpy.RipplesMQ.Client
RipplesMQ is a simple Message Broker, this library contain the client library, use
[Grumpy.RipplesMQ.Server](https://github.com/GrumpyBusted/Grumpy.RipplesMQ.Server) to setup RipplesMQ Server.
See [Grumpy.RipplesMQ.Sample](https://github.com/GrumpyBusted/Grumpy.RipplesMQ.Sample) for example.

This library makes it easy to use the RipplesMQ Message Broker, both for setting up handlers in services and
for using the message broker to publish messages or call requests.

Protocol for using RipplesMQ Client (The Message Bus):
1) Build Message Bus
2) Setup handlers (Subscribers and Request Handlers)
3) Start Message Bus
4) Publish and Request from the Message Bus
5) Dispose Message Bus

#### Create Message Bus
```csharp
MessageBus messageBus = new MessageBusBuilder().WithServiceName("MyService");
```
_ServiceName is defaulted to name of current process._

#### Setup Subscribe handler
```csharp
public void SetupSubscriber(IMessageBus messageBus) 
{
    var config = new PublishSubscribeConfig { Persistent = false, Topic = "MyTopicA" };

    messageBus.SubscribeHandler<MyDtoA>(config, MyTopicAHandler, "MySubscriberName");
}

public void MyTopicAHandler(MyDtoA dto, CancellationToken token)
{
    // Do stuff with dto
}
```
_The subscribe handler method can be static or member of a class, the CancellationToken is obtional.
It is recommented to define the DTO and config class in the API Definition of your service_

#### Setup Request handler
```csharp
public void SetupRequestHandler(IMessageBus messageBus) 
{
    var config = new RequestResponseConfig { Name = "MyRequesterA", MillisecondsTimeout = 100 };

    messageBus.RequestHandler<MyRequestDtoA, MyResponseDtoA>(config, MyRequestAHandler);
}

public MyResponseDtoA MyRequestAHandler(MyRequestDtoA dto, CancellationToken token)
{
    // Do stuff with dto
    return new MyResponseDtoA();
}
```
_The request handler method can be static or member of a class, the CancellationToken is obtional.
It is recommented to define the DTO and config class in the API Definition of your service_

#### Start Message Bus
```csharp
messageBus.Start(cancellationToken);
```

#### Publish a Message for a Subscriber
```csharp
var config = new PublishSubscribeConfig { Persistent = false, Topic = "MyTopicB" };

messageBus.Publish(config, new MyDtoB());
```

#### Invoke Request Handler 
```csharp
var config = new RequestResponseConfig { Name = "MyRequesterA", MillisecondsTimeout = 100 };

var message = messageBus.RequestAsync<MyRequestDtoB, MyResponseDtoB>(config, new MyRequestDtoB());

var response = message.Result;
```
_Result of RequestAsync is a Task, to get response get Result from the task. There is also a synchronous version just called Request._

#### Stop Message Bus
```csharp
messageBus.Stop();
messageBus.Dispose();
```
_Dispose will also stop, so no actual need to call Stop._
