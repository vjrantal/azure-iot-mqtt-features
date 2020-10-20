using System;
using System.IO;
using System.Threading;
using CrossCutting;

namespace IotHubConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var configuration = Configuration.BuildConfiguration();

            //Console.WriteLine("Enter message payload for C2D");
           // var input = Console.ReadLine();
            var consumer = new SenderConsumer(configuration);
            //consumer.SendCloudToDeviceMessageAsync(input).Wait();

            var recConsumer = new ReceiverConsumer(configuration);
            Console.WriteLine("IoT Hub Quickstarts - Read device to cloud messages. Ctrl-C to exit.\n");
            using var cancellationSource = new CancellationTokenSource();

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
