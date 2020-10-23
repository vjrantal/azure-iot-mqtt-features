using System;
using System.Threading.Tasks;
using CrossCutting;
using MQTTnet;

namespace MessageSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = Configuration.BuildConfiguration();
            var iotHubDeviceConnectionString = configuration["IotHubDeviceConnectionString"];
            var device = new Device(iotHubDeviceConnectionString);

            await device.ConnectDevice();
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            string input;
            do
            {
                Console.WriteLine("Enter message payload for D2C");
                input = Console.ReadLine();
                if (input != null)
                {
                    await device.SendDeviceToCloudMessageAsync(input);
                }
            } while (input != null);
        }

        public static void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
        }
    }
}
