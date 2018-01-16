using System.Threading;
using System.Threading.Tasks;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    internal class TestMessageQueue : ILocaleQueue
    {
        public TestMessageQueue(string name, bool durable, bool privateQueue)
        {
            Name = name;
            Durable = durable;
            Private = privateQueue;
        }

        public bool Durable { get; }
        
        public bool Transactional => true;
        
        public TestMessageBroker MessageBroker { private get; set; }
        
        public bool Private { get; }
        
        public T Receive<T>(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return default(T);
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
                    MessageBroker.RegisterPublish(message as PublishMessage);
                else if (typeof(T) == typeof(SubscribeHandlerCompleteMessage))
                    MessageBroker.RegisterSubscriberComplete(message as SubscribeHandlerCompleteMessage);
                else if (typeof(T) == typeof(SubscribeHandlerErrorMessage))
                    MessageBroker.RegisterSubscribeError(message as SubscribeHandlerErrorMessage);
                else if (typeof(T) == typeof(ResponseMessage))
                    MessageBroker.RegisterResponse(message as ResponseMessage);
            }
        }

        public Task<ITransactionalMessage> ReceiveAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return default(Task<ITransactionalMessage>);
        }

        public ITransactionalMessage Receive(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return default(ITransactionalMessage);
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