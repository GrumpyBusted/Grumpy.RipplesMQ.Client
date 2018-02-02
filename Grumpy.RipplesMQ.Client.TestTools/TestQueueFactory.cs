using System.Collections.Generic;
using System.Linq;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;

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
        public ILocaleQueue CreateLocale(string name, bool privateQueue, LocaleQueueMode localeQueueMode, bool transactional, AccessMode accessMode)
        {
            if (!_queues.Any(q => q.Name.Contains(name) || name.Contains(q.Name)))
                _queues.Add(new TestQueue(name, _testMessageBroker));

            return _queues.SingleOrDefault(q => q.Name.Contains(name) || name.Contains(q.Name));
        }

        /// <inheritdoc />
        public IRemoteQueue CreateRemote(string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional, AccessMode accessMode)
        {
            return null;
        }
    }
}