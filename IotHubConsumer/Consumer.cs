using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    public class Consumer : IConsumer
    {
        private ServiceClient serviceClient;
        private static string IotHubConnectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
        static string targetDevice = Environment.GetEnvironmentVariable("DeviceId");

        public void ConnectConsumer()
        {
            Console.WriteLine("Create connection");
            serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
        }

        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            Console.WriteLine("Prepare to send a C2D message.");
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
