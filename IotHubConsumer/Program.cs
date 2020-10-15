using System;

namespace IotHubConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter message payload for C2D");
            var input = Console.ReadLine();
            var consumer = new Consumer();
            consumer.SendCloudToDeviceMessageAsync(input).Wait();
        }
    }
}
