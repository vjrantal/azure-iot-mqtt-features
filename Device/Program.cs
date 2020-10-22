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

            Console.WriteLine("Enter message payload for D2C");
            var input = Console.ReadLine();
            await device.SendDeviceToCloudMessageAsync(input);
        }

        public static void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
        }
    }
}
