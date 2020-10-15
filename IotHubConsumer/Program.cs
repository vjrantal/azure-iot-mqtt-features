using System;
using System.IO;
using CrossCutting;

namespace IotHubConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = System.Reflection.Assembly.GetExecutingAssembly().Location;

            var configuration = Configuration.BuildConfiguration(Path.GetPathRoot(dir));

            Console.WriteLine("Enter message payload for C2D");
            var input = Console.ReadLine();
            var consumer = new Consumer(configuration);
            consumer.SendCloudToDeviceMessageAsync(input).Wait();
        }
    }
}
