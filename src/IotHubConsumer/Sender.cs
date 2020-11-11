using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    public class Sender
    {
        private readonly string deviceId;
        private ServiceClient serviceClient;

        public Sender(string iotHubConnectionString, string deviceId)
        {
            this.deviceId = deviceId;
            Console.WriteLine("Create connection");
            serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
        }

        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            Console.WriteLine("Prepare to send a C2D message.");
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }
    }
}
