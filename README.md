[![Build status](https://ci.appveyor.com/api/projects/status/9aiyqm4it2np1x3p?svg=true)](https://ci.appveyor.com/project/GrumpyBusted/grumpy-ripplesmq-client)
[![codecov](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Client/branch/master/graph/badge.svg)](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Client)
[![nuget](https://img.shields.io/nuget/v/Grumpy.RipplesMQ.Client.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Client/)
[![downloads](https://img.shields.io/nuget/dt/Grumpy.RipplesMQ.Client.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Client/)

# Grumpy.RipplesMQ.Client
RipplesMQ is a simple Message Broker, this library contain the client library, use
[Grumpy.RipplesMQ.Server](https://github.com/GrumpyBusted/Grumpy.RipplesMQ.Server) to setup RipplesMQ Server.

This library makes it easy to use the RipplesMQ Message Broker, both for setting up handlers in services and
for using the message broker to publish messages or call requests.

```csharp
public void Process()
{
    // Creating the Message Bus
    var messageBus = MessageBusBuilder.Build("MyService");

    // Setup SubscribeHandler to handle a publish message, each message coming on the queue will call MyTopicAHandler
    messageBus.SubscribeHandler<MyDtoA>(new PublishSubscribeConfig { Persistent = false, Topic = "MyTopicA" }, MyTopicAHandler, "MySubscriberName");

    // Setup RequestHandler to handle a requests
    messageBus.RequestHandler<MyRequestADto, MyResponseADto>(new RequestResponseConfig { Name = "MyRequesterA", MillisecondsTimeout = 100 }, MyRequestAHandler);

    // Start the Message bus after all handlers are configured
    messageBus.Start(_cancellationToken);

    // Publish MyDtoB on topic MyTopicB, will in a short time initiate all subscribe handlers listening for MyTopicB
    messageBus.Publish(new PublishSubscribeConfig { Persistent = true, Topic = "MyTopicB" }, new MyDtoB());

    // Request a response from a handler that can response to request name = "MyRequesterB" 
    var message = messageBus.RequestAsync<MyRequestBDto, MyResponseBDto>(new RequestResponseConfig { Name = "MyRequesterB", MillisecondsTimeout = 100 }, new MyRequestBDto);

    // Result of RequestAsync is a Task, to get response get Result from the task. There is also a synchronous version just called Request
    var response = message.Result;

    messageBus.Dispose();
}

public void MyTopicAHandler(MyDtoA dto)
{
    // Do stuff with dto
}

public MyResponseDtoA MyRequestAHandler(MyRequestDtoA dto)
{
    // Do stuff with dto

    return new MyResponseDtoA();
}
```
