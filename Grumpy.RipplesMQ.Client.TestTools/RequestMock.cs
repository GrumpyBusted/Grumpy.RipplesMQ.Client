using System;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    internal class RequestMock
    {
        public Func<object, bool> Function { get; set; }
        public object Response { get; set; }
        public Type ResponseType { get; set; }
    }
}