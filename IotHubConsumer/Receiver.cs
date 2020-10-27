using System;
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

        public async Task<HashSet<D2CMessage>> ReceiveMessagesFromDeviceAsync(CancellationToken cancellationToken)
        {
            var connectionString = iotHubSasKey == null
            ? eventHubCompatibleEndpoint
            : BuildEventHubsConnectionString(eventHubCompatibleEndpoint, iotHubSasKeyName, iotHubSasKey);

            await using var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, eventHubName);

            var receivedMessages = new HashSet<D2CMessage>();

            Console.WriteLine("Listening for messages on all partitions");

            try
            {
                await foreach (var partitionEvent in consumer.ReadEventsAsync(cancellationToken))
                {
                    Console.WriteLine("Message received on partition {0}:", partitionEvent.Partition.PartitionId);

                    var data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine("\t{0}:", data);

                    var retainFlag = "false";
                    if (partitionEvent.Data.Properties.ContainsKey("mqtt-retain"))
                    {
                        retainFlag = partitionEvent.Data.Properties["mqtt-retain"].ToString();
                    }

                    var messageType = "telemetry";
                    if (partitionEvent.Data.Properties.ContainsKey("iothub-MessageType"))
                    {
                        messageType = partitionEvent.Data.Properties["iothub-MessageType"].ToString();
                    }
                    receivedMessages.Add(new D2CMessage { Payload = data, RetainFlag = retainFlag, MessageType = messageType });

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
