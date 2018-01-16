using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Config;

namespace Grumpy.RipplesMQ.Client.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// You can not register two subscribe handlers with same name
    /// </summary>
    [Serializable]
    public sealed class DoubleSubscribeHandlerException : Exception
    {
        private DoubleSubscribeHandlerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        /// <summary>
        /// You can not register two subscribe handlers with same name and topic
        /// </summary>
        /// <param name="config">Publish/Subscribe Configuration</param>
        /// <param name="name">Subscriber name</param>
        /// <exception cref="T:System.NotImplementedException"></exception>
        public DoubleSubscribeHandlerException(PublishSubscribeConfig config, string name) : base("Double Subscribe Handler Exception")
        {
            Data.Add(nameof(config), config.TrySerializeToJson());
            Data.Add(nameof(name), name);
        }
    }
}