using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting;
using Microsoft.Extensions.Configuration;

namespace IotHubConsumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = Configuration.BuildConfiguration();
            await SendCloudToDeviceMessage(configuration);
            await ReceiveMessagesFromDevice(configuration);
        }

        public static async Task SendCloudToDeviceMessage(IConfiguration configuration)
        {
            Console.WriteLine("Enter message payload for C2D");
            var input = Console.ReadLine();

            var iotHubConnectionString = configuration["IotHubConnectionString"];
            var deviceId = configuration["DeviceId"];
            var consumer = new Sender(iotHubConnectionString, deviceId);
            await consumer.SendCloudToDeviceMessageAsync(input);
        }

        public static async Task ReceiveMessagesFromDevice(IConfiguration configuration)
        {
            Console.WriteLine("Start to read device to cloud messages. Ctrl-C to exit.\n");

            var eventHubCompatibleEndpoint = configuration["EventHubCompatibleEndpoint"];
            var eventHubName = configuration["EventHubName"];
            var iotHubSasKey = configuration["IotHubSasKey"];

            var recConsumer = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);

            var cancellationSource = new CancellationTokenSource();
            void cancelKeyPressHandler(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
                Console.WriteLine("Exiting...");

                Console.CancelKeyPress -= cancelKeyPressHandler;
            }
            Console.CancelKeyPress += cancelKeyPressHandler;

            await recConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);
        }
    }
}
