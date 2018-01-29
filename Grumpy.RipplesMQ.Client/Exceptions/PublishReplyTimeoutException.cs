using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Reply from Publish Timeout Exception
    /// </summary>
    [Serializable]
    public sealed class PublishReplyTimeoutException : Exception
    {
        private PublishReplyTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// Reply from Publish Timeout Exception
        /// </summary>
        /// <param name="publishMessage"></param>
        public PublishReplyTimeoutException(PublishMessage publishMessage) : base("Publish Reply Timeout Exception")
        {
            Data.Add(nameof(publishMessage), publishMessage?.TrySerializeToJson());
        }
    }
}