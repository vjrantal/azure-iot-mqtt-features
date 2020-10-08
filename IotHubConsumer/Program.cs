using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    class Program
    {
        static ServiceClient serviceClient;
        static string connectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
        static string targetDevice = Environment.GetEnvironmentVariable("DeviceId");
        static void Main(string[] args)
        {
            Console.WriteLine("Send Cloud-to-Device message\n");
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Console.WriteLine("Press any key to send a C2D message.");
            Console.ReadLine();
            SendCloudToDeviceMessageAsync().Wait();
            Console.ReadLine();
        }
        private async static Task SendCloudToDeviceMessageAsync()
        {
            var commandMessage = new
             Message(Encoding.ASCII.GetBytes("Cloud to device message."));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
