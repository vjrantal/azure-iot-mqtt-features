﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace IotHubConsumer
{
    public class Receiver
    {
        private const string iotHubSasKeyName = "service";
        private readonly string eventHubCompatibleEndpoint;
        private readonly string eventHubName;
        private readonly string iotHubSasKey;

        public Receiver(string eventHubCompatibleEndpoint, string eventHubName, string iotHubSasKey = null)
        {
            this.eventHubCompatibleEndpoint = eventHubCompatibleEndpoint;
            this.iotHubSasKey = iotHubSasKey;
            this.eventHubName = eventHubName;
        }

        public async Task<Dictionary<string, IDictionary<string, object>>> ReceiveMessagesFromDeviceAsync(CancellationToken cancellationToken)
        {
            var connectionString = iotHubSasKey == null
            ? eventHubCompatibleEndpoint
            : BuildEventHubsConnectionString(eventHubCompatibleEndpoint, iotHubSasKeyName, iotHubSasKey);

            await using var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, eventHubName);

            var receivedMessages = new Dictionary<string, IDictionary<string, object>>();

            Console.WriteLine("Listening for messages on all partitions");

            try
            {
                await foreach (var partitionEvent in consumer.ReadEventsAsync(cancellationToken))
                {
                    Console.WriteLine("Message received on partition {0}:", partitionEvent.Partition.PartitionId);

                    var data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine("\t{0}:", data);

                    receivedMessages.TryAdd(data, partitionEvent.Data.Properties);

                    Console.WriteLine("Application properties (set by device):");
                    foreach (var prop in partitionEvent.Data.Properties)
                    {
                        Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
                    }

                    Console.WriteLine("System properties (set by IoT Hub):");
                    foreach (var prop in partitionEvent.Data.SystemProperties)
                    {
                        Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
                    }
                }

            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }

            return receivedMessages;
        }

        private static string BuildEventHubsConnectionString(string eventHubsEndpoint, string iotHubSharedKeyName, string iotHubSharedKey)
        {
            return $"Endpoint={ eventHubsEndpoint };SharedAccessKeyName={ iotHubSharedKeyName };SharedAccessKey={ iotHubSharedKey }";
        }
    }
}
