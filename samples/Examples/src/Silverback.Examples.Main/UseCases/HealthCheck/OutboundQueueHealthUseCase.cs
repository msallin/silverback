﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.HealthChecks;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.HealthCheck
{
    public class OutboundQueueHealthUseCase : UseCase
    {
        public OutboundQueueHealthUseCase() : base("Outbound queue (deffered produce)", 20, 1)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToKafka(options => options
                    .AddDbOutboundConnector()
                    .AddDbOutboundWorker());
        }

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })              
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events-two")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-failure")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://somwhere:1000",
                        MessageTimeoutMs = 1000
                    }
                }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine("Checking outbound queue (maxAge: 100ms, maxQueueLength: 1)...");
            Console.ResetColor();

            var result = await new OutboundQueueHealthCheckService(
                    serviceProvider.GetRequiredService<IOutboundQueueConsumer>())
                .CheckIsHealthy(maxAge: TimeSpan.FromMilliseconds(100), maxQueueLength: 1);
            
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine($"Healthy: {result}");
            Console.ResetColor();
        }
    }
}