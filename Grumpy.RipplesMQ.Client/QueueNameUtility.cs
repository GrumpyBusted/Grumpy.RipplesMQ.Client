using System;
using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client
{
    /// <inheritdoc />
    public class QueueNameUtility : IQueueNameUtility
    {
        private const int MaxQueueLength = 99;
        private readonly string _serviceName;

        /// <inheritdoc />
        public QueueNameUtility(string serviceName)
        {
            _serviceName = serviceName.Replace("$", ".");
        }

        /// <inheritdoc />
        public string ReplyQueue<T>(string id = "")
        {
            return Build("Reply." + typeof(T).Name + (id.NullOrEmpty() ? "" : $".{id}"));
        }

        /// <inheritdoc />
        public string Build(string topic, string name, bool durable)
        {
            return Build($"{topic}.{name}", durable);
        }

        /// <inheritdoc />
        public string Build(string name, bool durable = false)
        {
            var queueName = _serviceName + "." + name;

            if (queueName.Length > MaxQueueLength)
                queueName = "RipplesMQ." + name;

            if (queueName.Length > MaxQueueLength && durable)
                throw new ArgumentException("Input to queue name too long", nameof(name));

            return Generate(queueName, durable);
        }

        private static string Generate(string queueName, bool durable)
        {
            var name = durable ? queueName : "#" + queueName;

            if (!durable)
            {
                var uniqueKey = UniqueKeyUtility.Generate();
                name = name.Left(MaxQueueLength - uniqueKey.Length - 1) + "." + uniqueKey;
            }

            return name;
        }
    }
}
