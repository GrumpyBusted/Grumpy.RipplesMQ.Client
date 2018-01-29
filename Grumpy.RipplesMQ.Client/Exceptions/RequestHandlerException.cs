using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Exception in Request Handler
    /// </summary>
    [Serializable]
    public sealed class RequestHandlerException : Exception
    {
        private RequestHandlerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Exception in Request Handler
        /// </summary>
        /// <param name="requestMessage">Request message</param>
        /// <param name="responseErrorMessage">Response error message</param>
        public RequestHandlerException(RequestMessage requestMessage, ResponseErrorMessage responseErrorMessage) : base("Exception in Request Handler", responseErrorMessage.Exception)
        {
            Data.Add(nameof(requestMessage), requestMessage?.TrySerializeToJson());
            Data.Add(nameof(responseErrorMessage), responseErrorMessage.TrySerializeToJson());
        }
    }
}