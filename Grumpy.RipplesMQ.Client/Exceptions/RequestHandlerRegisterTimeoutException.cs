using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Timeout registration Request Handler
    /// </summary>
    [Serializable]
    public sealed class RequestHandlerRegisterTimeoutException : Exception
    {
        private RequestHandlerRegisterTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Timeout registration Request Handler
        /// </summary>
        /// <param name="requestHandlerRegisterMessage">Request Handler Registration Message</param>
        public RequestHandlerRegisterTimeoutException(RequestHandlerRegisterMessage requestHandlerRegisterMessage) : base("Register of Message Bus Request Handler Timeout Exception")
        {
            Data.Add(nameof(requestHandlerRegisterMessage), requestHandlerRegisterMessage.TrySerializeToJson());
        }
    }
}