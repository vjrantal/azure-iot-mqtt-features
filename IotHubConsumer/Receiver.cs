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
                // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
                // events to become available.  Reading can be canceled by breaking out of the loop when an event is processed or by
                // signaling the cancellation token.
                //
                // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
                // and samples.  For real-world production scenarios, it is strongly recommended that you consider using the
                // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
                //
                // More information on the "EventProcessorClient" and its benefits can be found here:
                //   https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md


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

    public class D2CMessage
    {
        public string Payload { get; set; }
        public string RetainFlag { get; set; }
    }
}
