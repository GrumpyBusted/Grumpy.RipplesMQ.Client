using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Request/Response Timeout Exception
    /// </summary>
    [Serializable]
    public sealed class RequestResponseTimeoutException : Exception
    {
        private RequestResponseTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Request/Response Timeout Exception
        /// </summary>
        /// <param name="requestMessage">Request Message</param>
        /// <param name="millisecondsTimeout">Timeout interval in Milliseconds</param>
        public RequestResponseTimeoutException(RequestMessage requestMessage, int millisecondsTimeout) : base("Request Timeout before Response Exception")
        {
            Data.Add(nameof(requestMessage), requestMessage.TrySerializeToJson());
            Data.Add(nameof(millisecondsTimeout), millisecondsTimeout);
        }
    }
}