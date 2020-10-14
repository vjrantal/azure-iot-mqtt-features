using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MessageSample
{
    public class Device
    {
        // String containing Hostname, Device Id, Module Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_MODULE_CONN_STRING environment variable
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string[] connectionString = Environment.GetEnvironmentVariable("IotHubConnectionString").Split(';');
        private static string deviceId = connectionString[1].Split('=', 2)[1];
        private static string sharedAccessKey = connectionString[2].Split('=', 2)[1];
        private static string hubAddress = connectionString[0].Split('=', 2)[1];
        private readonly IMqttClient mqttClient;

        public Device()
        {
            var factory = new MqttFactory();
            this.mqttClient = factory.CreateMqttClient();
        }

        public async Task RunSampleAsync()
        {
            await ConnectDevice();
            await GetEventAsync();
            await SendEventAsync();
        }

        private async Task ConnectDevice()
        {
            var username = hubAddress + "/" + deviceId;
            var password = GenerateSasToken(hubAddress + "/devices/" + deviceId, sharedAccessKey);
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(hubAddress, 8883)
                .WithCredentials(username, password)
                .WithClientId(deviceId)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTls()
                .Build();


            await mqttClient.ConnectAsync(options, CancellationToken.None);
            mqttClient.UseDisconnectedHandler(new MqttClientDisconnectedHandlerDelegate(e => Disconnected(e, options)));
        }

        private async Task SendEventAsync()
        {
            var topicD2C = $"devices/{deviceId}/messages/events/";

            while (true)
            {
                var payloadJObject = new JObject();

                payloadJObject.Add("OfficeTemperature", "22." + DateTime.UtcNow.Millisecond.ToString());
                payloadJObject.Add("OfficeHumidity", (DateTime.UtcNow.Second + 40).ToString());

                string payload = JsonConvert.SerializeObject(payloadJObject);
                Console.WriteLine($"Topic:{topicD2C} Payload:{payload}");

                var message = new MqttApplicationMessageBuilder()
                   .WithTopic(topicD2C)
                   .WithPayload(payload)
                   .WithAtLeastOnceQoS()
                .Build();

                Console.WriteLine("PublishAsync start");
                await mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine("PublishAsync finish");

                Thread.Sleep(30100);
            }
        }

        private async Task GetEventAsync()
        {
            var topicC2D = $"devices/{deviceId}/messages/devicebound/#";

            mqttClient.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(e => ApplicationMessageReceived(e)));
            await mqttClient.SubscribeAsync(topicC2D, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
        }

        private static void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
        }

        private async void Disconnected(MqttClientDisconnectedEventArgs e, IMqttClientOptions options)
        {
            Console.WriteLine("Disconnected");

            try
            {
                Console.WriteLine("Trying to reconnect");

                await mqttClient.ConnectAsync(options, CancellationToken.None);
            }
            catch
            {
                Console.WriteLine("### RECONNECTING FAILED ###");
            }
        }

        private static string GenerateSasToken(string resourceUri, string key, int expiryInSeconds = 36000)
        {
            var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + expiryInSeconds);
            var stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            var hmac = new HMACSHA256(Convert.FromBase64String(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry);
            return sasToken;
        }
    }
}
