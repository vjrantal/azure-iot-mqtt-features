using System;
using System.IO;
using System.Threading;
using CrossCutting;
using Microsoft.Extensions.Configuration;

namespace IotHubConsumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = Configuration.BuildConfiguration();
            SendCloudToDeviceMessage(configuration);
            ReceiveMessagesFromDevice(configuration);
        }

        public static void SendCloudToDeviceMessage(IConfiguration configuration)
        {
            Console.WriteLine("Enter message payload for C2D");
            var input = Console.ReadLine();

            var iotHubConnectionString = configuration["IotHubConnectionString"];
            var deviceId = configuration["DeviceId"];
            var consumer = new Sender(iotHubConnectionString, deviceId);
            consumer.SendCloudToDeviceMessageAsync(input).Wait();
        }

        public static void ReceiveMessagesFromDevice(IConfiguration configuration)
        {
            Console.WriteLine("Start to read device to cloud messages. Ctrl-C to exit.\n");

            var eventHubCompatibleEndpoint = configuration["EventHubCompatibleEndpoint"];
            var eventHubName = configuration["EventHubName"];
            var recConsumer = new Receiver(eventHubCompatibleEndpoint, eventHubName);

            var cancellationSource = new CancellationTokenSource();
            void cancelKeyPressHandler(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
                Console.WriteLine("Exiting...");

                Console.CancelKeyPress -= cancelKeyPressHandler;
            }
            Console.CancelKeyPress += cancelKeyPressHandler;

            recConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token).Wait();
        }
    }
}
