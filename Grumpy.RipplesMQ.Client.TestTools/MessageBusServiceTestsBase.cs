using System;
using System.Threading;
using Grumpy.RipplesMQ.Client.Interfaces;

namespace Grumpy.RipplesMQ.Client.TestTools
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for Component Test of Message Bus Services
    /// </summary>
    public abstract class MessageBusServiceTestsBase : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Test Mock of the Message Broker
        /// </summary>
        protected TestMessageBroker MessageBroker { get; }

        /// <summary>
        /// Message Bus
        /// </summary>
        protected IMessageBus MessageBus { get; }

        /// <summary>
        /// Create Message Bus Base Class Object for Component Testing of Services using Message Bus and Message Broker
        /// </summary>
        protected MessageBusServiceTestsBase()
        {
            MessageBroker = new TestMessageBroker();
            MessageBus = MessageBroker.MessageBus;
        }

        /// <summary>
        /// Start the test Message Bus
        /// </summary>
        protected void Start()
        {
            MessageBus.Start(new CancellationToken(), false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]  
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    MessageBus?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}