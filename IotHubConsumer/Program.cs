using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Send Cloud-to-Device message\n");
            var consumer = new IotHubConsumer();
            consumer.ConnectConsumer();
            Console.WriteLine("Press any key to send a C2D message.");
            Console.ReadLine();
            consumer.SendCloudToDeviceMessageAsync("Cloud to device message.").Wait();
            Console.ReadLine();
        }

    }
}
