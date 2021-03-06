using System;
using System.Diagnostics.CodeAnalysis;
using Grumpy.Json;
using Grumpy.MessageQueue.Interfaces;
using Newtonsoft.Json;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    /// <inheritdoc />
    public class TestTransactionalMessage : ITransactionalMessage
    {
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        public void Ack()
        {
        }

        /// <inheritdoc />
        public void NAck()
        {
        }

        /// <inheritdoc />
        public object Message { get; }

        /// <inheritdoc />
        public string Body { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public TestTransactionalMessage(object message)
        {
            Message = message;
            Body = message.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            Type = message.GetType();
        }
    }
}