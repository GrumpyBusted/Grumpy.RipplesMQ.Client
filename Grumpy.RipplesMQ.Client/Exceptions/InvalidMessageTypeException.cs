using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Invalid Message Type Received
    /// </summary>
    [Serializable]
    public sealed class InvalidMessageTypeException : Exception
    {
        private InvalidMessageTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type Received
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="exceptedType">Excepted Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        /// <exception cref="T:System.NotImplementedException"></exception>
        public InvalidMessageTypeException(object message, Type exceptedType, string actualType) : base("Invalid Message Type Received")
        {
            Data.Add(nameof(message), message?.TrySerializeToJson());
            Data.Add(nameof(exceptedType), exceptedType);
            Data.Add(nameof(actualType), actualType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type Received
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="exceptedType">Excepted Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        /// <exception cref="T:System.NotImplementedException"></exception>
        public InvalidMessageTypeException(object message, Type exceptedType, Type actualType) : base("Invalid Message Type Received")
        {
            Data.Add(nameof(message), message?.TrySerializeToJson());
            Data.Add(nameof(exceptedType), exceptedType);
            Data.Add(nameof(actualType), actualType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type Received
        /// </summary>
        /// <param name="request">Request Message</param>
        /// <param name="response">Response Message</param>
        /// <param name="exceptedType">Excepted Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        public InvalidMessageTypeException(object request, object response, Type exceptedType, Type actualType) : base("Invalid Message Type Received")
        {
            Data.Add(nameof(request), request?.TrySerializeToJson());
            Data.Add(nameof(response), response?.TrySerializeToJson());
            Data.Add(nameof(exceptedType), exceptedType);
            Data.Add(nameof(actualType), actualType);
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type Exception
        /// </summary>
        /// <param name="requestMessage">Request Message Object</param>
        /// <param name="responseMessage">Response Message Object</param>
        /// <param name="expectedType">Expected Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        public InvalidMessageTypeException(RequestMessage requestMessage, ResponseMessage responseMessage, Type expectedType, Type actualType) : base("Invalid Response Message Type for Request Exception")
        {
            Data.Add(nameof(requestMessage), requestMessage?.TrySerializeToJson());
            Data.Add(nameof(responseMessage), responseMessage?.TrySerializeToJson());
            Data.Add(nameof(expectedType), expectedType);
            Data.Add(nameof(actualType), actualType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Invalid Message Type Exception
        /// </summary>
        /// <param name="requestMessage">Request Message Object</param>
        /// <param name="expectedType">Expected Message Type</param>
        /// <param name="actualType">Actual Message Type</param>
        public InvalidMessageTypeException(RequestMessage requestMessage, Type expectedType, Type actualType) : base("Invalid Request Message Type Exception")
        {
            Data.Add(nameof(requestMessage), requestMessage?.TrySerializeToJson());
            Data.Add(nameof(expectedType), expectedType.SerializeToJson());
            Data.Add(nameof(actualType), actualType.SerializeToJson());
        }
    }
}