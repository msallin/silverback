﻿using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that publishes the outgoing messages to the message queue
    /// (either forwarding them directly to the message broker or storing them in the outbox table).
    /// </summary>
    public interface IOutboundAdapter
    {
        /// <summary>
        /// Publishes the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" /> to the specified <see cref="IEndpoint" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <param name="endpoint">The endpoint.</param>
        void Relay(IIntegrationMessage message, IEndpoint endpoint);
    }
}