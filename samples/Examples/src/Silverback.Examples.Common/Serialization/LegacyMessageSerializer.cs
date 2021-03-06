﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using Newtonsoft.Json;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Common.Serialization
{
    public class LegacyMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None
        };

        public byte[] Serialize(object message, MessageHeaderCollection messageHeaders) =>
            Encoding.ASCII.GetBytes(
                JsonConvert.SerializeObject(message, _settings));

        public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders) =>
            JsonConvert.DeserializeObject<LegacyMessage>(
                Encoding.ASCII.GetString(message));
    }
}