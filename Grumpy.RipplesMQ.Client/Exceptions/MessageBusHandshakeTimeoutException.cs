using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Timeout sending handshake to message broker
    /// </summary>
    [Serializable]
    public sealed class MessageBusHandshakeTimeoutException : Exception
    {
        private MessageBusHandshakeTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Timeout sending handshake to message broker
        /// </summary>
        /// <param name="messageBusServiceHandshakeMessage">Message Bus Handshake Message</param>
        public MessageBusHandshakeTimeoutException(MessageBusServiceHandshakeMessage messageBusServiceHandshakeMessage) : base("Sending Message Bus Handshake Timeout Exception")
        {
            Data.Add(nameof(messageBusServiceHandshakeMessage), messageBusServiceHandshakeMessage?.TrySerializeToJson());
        }
    }
}