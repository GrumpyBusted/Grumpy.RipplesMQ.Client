using System;
using System.Threading;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client.TestTools.UnitTests.Helper
{
    internal class MyTestService
    {
        private readonly IMessageBus _messageBus;

        public MyTestService(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public string Name { get; private set; }

        public void DoStuffAndPublishMessage(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                var myPublishDto = new MyPublishDto();

                _messageBus.Publish(MyTestServiceConfig.MyTestPublish, myPublishDto);
            }
        }

        public void MyTestSubscribeHandler(MySubscribeDto mySubscribeDto, CancellationToken cancellationToken)
        {
            if (mySubscribeDto.Name == "Exception")
                throw new Exception(mySubscribeDto.Name);

            var myPublishDto = new MyPublishDto();

            _messageBus.Publish(MyTestServiceConfig.MyTestPublish, myPublishDto);
        }

        public void DoStuffAndRequestData()
        {
            var myRequestDto = new MyRequestDto
            {
                Count = 1,
                Name = "MyName"
            };

            var myResponseDto = _messageBus.Request<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, myRequestDto);

             Name = myResponseDto.Name;
        }

        public void DoStuffAndRequestAsyncData()
        {
            var myRequestDto = new MyRequestDto
            {
                Count = 1,
                Name = "MyName"
            };

            var myResponseDto = _messageBus.RequestAsync<MyRequestDto, MyResponseDto>(MyTestServiceConfig.MyTestRequest, myRequestDto).Result;

            Name = myResponseDto.Name;
        }

        public MyResponseDto MyTestRequestHandler(MyRequestDto request, CancellationToken cancellationToken)
        {
            Name = request.Name;

            return new MyResponseDto { Count = request.Count, Name = request.Name };
        }
    }
}