using System;
using System.Runtime.Serialization;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Message Broker Server Missing Exception
    /// </summary>
    [Serializable]
    public sealed class MessageBrokerException : Exception
    {
        private MessageBrokerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Message Broker Server Missing Exception
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        public MessageBrokerException(string queueName) : base("Message Broker Queue not Found, Start Message Broker before server")
        {
            Data.Add(nameof(queueName), queueName);
        }
    }
}