using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Timeout Registering Subscribe Handler
    /// </summary>
    [Serializable]
    public sealed class SubscribeHandlerRegisterTimeoutException : Exception
    {
        private SubscribeHandlerRegisterTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Timeout Registering Subscribe Handler
        /// </summary>
        /// <param name="subscribeHandlerRegisterMessage"></param>
        public SubscribeHandlerRegisterTimeoutException(SubscribeHandlerRegisterMessage subscribeHandlerRegisterMessage) : base("Register of Message Bus Subscribe Handler Timeout Exception")
        {
            Data.Add(nameof(subscribeHandlerRegisterMessage), subscribeHandlerRegisterMessage.TrySerializeToJson());
        }
    }
}

