using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace IotHubConsumer
{
    public class SenderConsumer
    {
        private readonly IConfiguration configuration;
        private ServiceClient serviceClient;

        public SenderConsumer(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConnectConsumer()
        {
            Console.WriteLine("Create connection");
            serviceClient = ServiceClient.CreateFromConnectionString(configuration["IotHubConnectionString"]);
        }

        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            var targetDevice = configuration["DeviceId"];
            Console.WriteLine("Prepare to send a C2D message.");
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
