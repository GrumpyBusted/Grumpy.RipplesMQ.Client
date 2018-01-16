using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client.TestTools.UnitTests.Helper
{
    internal static class MyTestServiceConfig
    {
        public static readonly PublishSubscribeConfig MyTestPublish = new PublishSubscribeConfig
        {
            Topic = "MyPublishTopic",
            Persistent = false
        };

        public static readonly PublishSubscribeConfig MyTestSubscribe = new PublishSubscribeConfig
        {
            Topic = "MySubscribeTopic",
            Persistent = false
        };

        public static readonly RequestResponseConfig MyTestRequest = new RequestResponseConfig
        {
            Name = "MyRequest", 
            MillisecondsTimeout = 1000
        };
    }
}