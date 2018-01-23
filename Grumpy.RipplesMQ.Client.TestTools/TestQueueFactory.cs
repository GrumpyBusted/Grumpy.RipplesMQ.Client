using System;
using System.Collections.Generic;
using System.Linq;
using Grumpy.Common.Extensions;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    /// <inheritdoc />
    public class TestQueueFactory : IQueueFactory
    {
        private readonly List<ILocaleQueue> _queues;
        private readonly TestMessageBroker _testMessageBroker;

        /// <inheritdoc />
        public TestQueueFactory(TestMessageBroker testMessageBroker)
        {
            _testMessageBroker = testMessageBroker;
            _queues = new List<ILocaleQueue>();
        }

        /// <inheritdoc />
        public ILocaleQueue CreateLocale(string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional)
        {
            if (!_queues.Any(q => q.Name.Contains(name) || name.Contains(q.Name)))
                _queues.Add(new TestQueue(name, _testMessageBroker));

            return _queues.SingleOrDefault(q => q.Name.Contains(name) || name.Contains(q.Name));
        }

        /// <inheritdoc />
        public IRemoteQueue CreateRemote(string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional)
        {
            return null;
        }
    }
}