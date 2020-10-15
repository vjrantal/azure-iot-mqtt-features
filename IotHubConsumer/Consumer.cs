using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    public class Consumer : IConsumer
    {
        private ServiceClient serviceClient;
        private static string fullConnectionString = Environment.GetEnvironmentVariable("FullIotHubConnectionString");
        static string targetDevice = Environment.GetEnvironmentVariable("DeviceId");

        public void ConnectConsumer()
        {
            Console.WriteLine("Create connection");
            serviceClient = ServiceClient.CreateFromConnectionString(fullConnectionString);
        }

        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            Console.WriteLine("Prepare to send a C2D message.");
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
