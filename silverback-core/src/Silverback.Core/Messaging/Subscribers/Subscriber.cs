﻿using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="ISubscriber" />
    public abstract class Subscriber<TMessage> : SubscriberBase<TMessage>
        where TMessage : IMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        protected Subscriber(Func<TMessage, bool> filter = null)
            : base(filter)
        {
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" /> asynchronously.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        public sealed override Task HandleAsync(TMessage message)
        {
            Handle(message);
            return Task.CompletedTask;
        }
    }
}