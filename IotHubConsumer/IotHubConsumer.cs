using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    class IotHubConsumer
    {
        private ServiceClient serviceClient;
        static string connectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
        static string targetDevice = Environment.GetEnvironmentVariable("DeviceId");

        public void ConnectConsumer()
        {
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }
        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
