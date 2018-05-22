using Grumpy.Common;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Client.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grumpy.RipplesMQ.Client
{
    /// <summary>
    /// Message Bus Builder
    /// </summary>
    public class MessageBusBuilder
    {
        private readonly IProcessInformation _processInformation;
        public string ServiceName;
        private ILogger _logger;

        /// <inheritdoc />
        public MessageBusBuilder()
        {
            _processInformation = new ProcessInformation();
            ServiceName = _processInformation.ProcessName;
            _logger = NullLogger.Instance;
        }

        /// <summary>
        /// Set Logger
        /// </summary>
        /// <param name="logger">The Logger</param>
        /// <returns></returns>
        public MessageBusBuilder WithLogger(ILogger logger)
        {
            _logger = logger;

            return this;
        }

        /// <summary>
        /// Build Message Bus
        /// </summary>
        /// <returns></returns>
        public IMessageBus Build()
        {
            var messageBusConfig = new MessageBusConfig
            {
                ServiceName = ServiceName
            };

            var queueFactory = new QueueFactory(_logger);
            var queueNameUtility = new QueueNameUtility(messageBusConfig.ServiceName);
            var messageBroker = new MessageBroker(_logger, messageBusConfig, queueFactory, _processInformation, queueNameUtility);
            var queueHandlerFactory = new QueueHandlerFactory(_logger, queueFactory);

            return new MessageBus(_logger, messageBroker, queueHandlerFactory, queueNameUtility);
        }

        /// <summary>
        /// Build Message Bus
        /// </summary>
        /// <param name="messageBusBuilder"></param>
        /// <returns>The Message Bus</returns>
        public static implicit operator MessageBus(MessageBusBuilder messageBusBuilder)
        {
            return (MessageBus)messageBusBuilder.Build();
        }
    }

    public static class MessageBusBuilderExtensions
    {
        /// <summary>
        /// Set Service Name
        /// </summary>
        /// <param name="serviceName">Service Name</param>
        /// <returns></returns>
        public static MessageBusBuilder WithServiceName(this MessageBusBuilder mbb, string serviceName)
        {
            mbb.ServiceName = serviceName;

            return mbb;
        }

    }
}