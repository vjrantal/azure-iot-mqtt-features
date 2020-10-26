using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;

namespace MessageSample
{
    public class Device
    {
        private readonly IMqttClient mqttClient;
        private readonly string sharedAccessKey;
        private readonly string hubAddress;
        private readonly string deviceId;

        public Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; set; }

        public Device(string iotHubDeviceConnectionString)
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var connectionString = iotHubDeviceConnectionString.Split(';');
            hubAddress = connectionString[0].Split('=', 2)[1];
            deviceId = connectionString[1].Split('=', 2)[1];
            sharedAccessKey = connectionString[2].Split('=', 2)[1];
        }

        public async Task ConnectDevice()
        {
            var username = hubAddress + "/" + deviceId;
            var password = GenerateSasToken(hubAddress + "/devices/" + deviceId, sharedAccessKey);
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(hubAddress, 8883)
                .WithCredentials(username, password)
                .WithClientId(deviceId)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithCleanSession(false)
                .WithTls()
                .Build();
            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }
        }

        public async Task DisconnectDevice()
        {
            await mqttClient.DisconnectAsync();
        }

        public MqttApplicationMessage ConstructMessage(string topic, string payload, bool retainFlag = false)
        {
            Console.WriteLine($"Topic:{topic} Payload:{payload}");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retainFlag)
                .WithAtMostOnceQoS()
                .Build();

            return message;
        }


        public async Task SendDeviceToCloudMessageAsync(string payload, bool retainFlag = false)
        {
            var topicD2C = $"devices/{deviceId}/messages/events/$.ct=application%2Fjson&$.ce=utf-8";
            var message = ConstructMessage(topicD2C, payload, retainFlag);

            Console.WriteLine("PublishAsync start");
            await mqttClient.PublishAsync(message, CancellationToken.None);
            Console.WriteLine("PublishAsync finish");
        }

        public async Task SubscribeToEventAsync(Action<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived)
        {
            var topicC2D = $"devices/{deviceId}/messages/devicebound/#";

            mqttClient.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(e => applicationMessageReceived(e)));
            await mqttClient.SubscribeAsync(topicC2D, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
        }

        private static string GenerateSasToken(string resourceUri, string key, int expiryInSeconds = 36000)
        {
            var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + expiryInSeconds);
            var stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            var hmac = new HMACSHA256(Convert.FromBase64String(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry);
            return sasToken;
        }
    }
}
