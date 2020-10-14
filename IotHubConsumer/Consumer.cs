using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IotHubConsumer
{
    public class Consumer:IConsumer
    {
        private ServiceClient serviceClient;
        private static string fullConnectionString = Environment.GetEnvironmentVariable("FullIotHubConnectionString");
        private static string[] connectionString = Environment.GetEnvironmentVariable("IotHubConnectionString").Split(';');
        private string targetDevice = connectionString[1].Split('=', 2)[1];

        public async Task SendCloudToDeviceMessageAsync(string message)
        {
            Console.WriteLine("Create connection"); 
            serviceClient = ServiceClient.CreateFromConnectionString(fullConnectionString); //TODO: extract separate method

            Console.WriteLine("Prepare to send a C2D message.");
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}
