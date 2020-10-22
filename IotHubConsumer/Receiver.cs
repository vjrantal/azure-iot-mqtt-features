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
        private readonly string eventHubCompatibleEndpoint;
        private readonly string eventHubName;

        public HashSet<D2CMessage> ReceivedMessages { get; set; } = new HashSet<D2CMessage>();

        public Receiver(string eventHubCompatibleEndpoint, string eventHubName)
        {
            this.eventHubCompatibleEndpoint = eventHubCompatibleEndpoint;
            this.eventHubName = eventHubName;
        }

        public async Task ReceiveMessagesFromDeviceAsync(CancellationToken cancellationToken)
        {
            await using var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, eventHubCompatibleEndpoint, eventHubName);

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

                    ReceivedMessages.Add(new D2CMessage { Payload = data, RetainFlag = retainFlag });

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
        }
    }
}
