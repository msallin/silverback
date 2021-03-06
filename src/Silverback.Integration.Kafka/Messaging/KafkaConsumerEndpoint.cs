﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaConsumerEndpoint : KafkaEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        public KafkaConsumerEndpoint(params string[] names) : base("")
        {
            Names = names;

            if (names == null)
                return;

            Name = names.Length > 1
                ? "[" + string.Join(",", names) + "]"
                : names[0];
        }
        public string[] Names { get; }

        public KafkaConsumerConfig Configuration { get; set; } = new KafkaConsumerConfig();

        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        #region Equality

        public bool Equals(KafkaConsumerEndpoint other) =>
            base.Equals(other) && Equals(Configuration, other?.Configuration);

        public override bool Equals(object obj) =>
            base.Equals(obj) && obj is KafkaConsumerEndpoint endpoint && Equals(endpoint);

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}