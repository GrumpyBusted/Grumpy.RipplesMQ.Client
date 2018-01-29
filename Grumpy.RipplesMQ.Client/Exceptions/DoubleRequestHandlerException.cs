using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// You can not register two request handlers with same name
    /// </summary>
    [Serializable]
    public sealed class DoubleRequestHandlerException : Exception
    {
        private DoubleRequestHandlerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// You can not register two request handlers with same name
        /// </summary>
        /// <param name="config">Request/Response Configuration</param>
        /// <exception cref="T:System.NotImplementedException"></exception>
        public DoubleRequestHandlerException(RequestResponseConfig config) : base("Double Request Handler Exception")
        {
            Data.Add(nameof(config), config?.TrySerializeToJson());
        }
    }
}