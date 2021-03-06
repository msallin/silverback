﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Behaviors
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class OutboundRoutingBehavior : IBehavior, ISorted
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly ConcurrentBag<object> _inboundMessagesCache = new ConcurrentBag<object>();

        public OutboundRoutingBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _messageKeyProvider = serviceProvider.GetRequiredService<MessageKeyProvider>();
        }

        public int SortIndex { get; } = 100;

        public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            
            messages.OfType<IInboundMessage>().ForEach(message => _inboundMessagesCache.Add(message.Content));

            var routedMessages = await WrapAndRepublishRoutedMessages(messages);
            
            if (!_routing.PublishOutboundMessagesToInternalBus)
                messages = messages.Where(m => !routedMessages.Contains(m)).ToList();

            return await next(messages);
        }

        private async Task<IEnumerable<object>> WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var wrappedMessages = messages
                .Where(message => !(message is IOutboundMessageInternal) &&
                                  !_inboundMessagesCache.Contains(message))
                .SelectMany(message =>
                    _routing
                        .GetRoutesForMessage(message)
                        .Select(route => 
                            CreateOutboundMessage(message, route)));

            if (wrappedMessages.Any())
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(wrappedMessages);

            return wrappedMessages.Select(m => m.Content);
        }
        private IOutboundMessage CreateOutboundMessage(object message, IOutboundRoute route)
        {
            var wrapper = (IOutboundMessage)Activator.CreateInstance(
                typeof(OutboundMessage<>).MakeGenericType(message.GetType()),
                message, null, route);

            _messageKeyProvider.EnsureKeyIsInitialized(wrapper.Content, wrapper.Headers);

            return wrapper;
        }
    }
}