using System.Threading;
using System.Threading.Tasks;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    internal class TestQueue : ILocaleQueue
    {
        private readonly TestMessageBroker _testMessageBroker;
        private object _message;
        private bool _onlyOnce;

        public void SetMessage<T>(T message, bool onlyOnce)
        {
            _onlyOnce = onlyOnce;
            _message = message;
        }

        public TestQueue(string name, TestMessageBroker testTestMessageBroker)
        {
            _testMessageBroker = testTestMessageBroker;
            Name = name;
            Durable = false;
            Private = true;
        }

        public bool Durable { get; }

        public bool Transactional => true;


        public bool Private { get; }

        public T Receive<T>(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return (T)Receive(millisecondsTimeout, cancellationToken).Message;
        }

        public string Name { get; }

        public int Count => 0;

        public void Connect()
        {
        }

        public void Connect(AccessMode mode)
        {
        }

        public void Reconnect()
        {
        }

        public void Reconnect(AccessMode mode)
        {
        }

        public void Disconnect()
        {
        }

        public void Send<T>(T message)
        {
            if (message != null)
            {
                if (typeof(T) == typeof(PublishMessage))
                    _testMessageBroker.RegisterPublish(message as PublishMessage);
                else if (typeof(T) == typeof(SubscribeHandlerCompleteMessage))
                    _testMessageBroker.RegisterSubscriberComplete(message as SubscribeHandlerCompleteMessage);
                else if (typeof(T) == typeof(SubscribeHandlerErrorMessage))
                    _testMessageBroker.RegisterSubscribeError(message as SubscribeHandlerErrorMessage);
                else if (typeof(T) == typeof(ResponseMessage))
                    _testMessageBroker.RegisterResponse(message as ResponseMessage);
            }
        }

        public Task<ITransactionalMessage> ReceiveAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (_message == null)
            {
                Thread.Sleep(1000);

                return  Task.FromResult<ITransactionalMessage>(new TransactionalMessage(null, null));
            }

            var res = Task.FromResult<ITransactionalMessage>(new TestTransactionalMessage(_message));

            if (_onlyOnce)
                _message = null;

            return res;
        }

        public ITransactionalMessage Receive(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return ReceiveAsync(millisecondsTimeout, cancellationToken).Result;
        }

        public void Create()
        {
        }

        public void Delete()
        {
        }

        public bool Exists()
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}