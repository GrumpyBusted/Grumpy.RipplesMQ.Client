using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Message Bus Service Registration Timeout Exception
    /// </summary>
    [Serializable]
    public sealed class MessageBusServiceRegisterTimeoutException : Exception
    {
        private MessageBusServiceRegisterTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Message Bus Service Registration Timeout Exception
        /// </summary>
        /// <param name="messageBusServiceRegisterMessage">Message Bus Service Registration Message</param>
        public MessageBusServiceRegisterTimeoutException(MessageBusServiceRegisterMessage messageBusServiceRegisterMessage) : base("Message Broker Client/Message Bus Service Register Timeout Exception")
        {
            Data.Add(nameof(messageBusServiceRegisterMessage), messageBusServiceRegisterMessage.TrySerializeToJson());
        }
    }
}