using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
using MQTTnet.Protocol;

namespace Client
{
    public class Device
    {
        private readonly IMqttClient mqttClient;
        private readonly string sharedAccessKey;
        private readonly string hubAddress;
        private readonly string deviceId;
        private readonly string topicD2C;

        public Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; set; }
        public List<X509Certificate> Certificates = new List<X509Certificate>
        {
           new X509Certificate2("CA-Certificate.pfx", "1234")
        };

        public Device(string iotHubDeviceConnectionString)
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var connectionString = iotHubDeviceConnectionString.Split(';');
            hubAddress = connectionString[0].Split('=', 2)[1];
            deviceId = connectionString[1].Split('=', 2)[1];
            sharedAccessKey = connectionString[2].Split('=', 2)[1];

            topicD2C = $"devices/{deviceId}/messages/events/$.ct=application%2Fjson&$.ce=utf-8";
        }

        public async Task ConnectDevice(string willPayload = "")
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
                .WithWillMessage(ConstructWillMessage(willPayload))
                .Build();
            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
            }
            catch (TaskCanceledException e)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
                Console.WriteLine(e.ToString());
            }
        }

        public async Task DisconnectDevice()
        {
            await mqttClient.DisconnectAsync();
        }

        public async Task ConnectDeviceUsingCACertificate()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(hubAddress, 8883)
                .WithCredentials(new MqttClientCredentials() { Username = hubAddress + "/" + deviceId })
                .WithClientId(deviceId)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    Certificates = Certificates
                })
                .Build();

            await mqttClient.ConnectAsync(options, CancellationToken.None);
        }


        public MqttApplicationMessage ConstructMessage(string topic, string payload, bool retainFlag = false, MqttQualityOfServiceLevel mqttQoSLevel = MqttQualityOfServiceLevel.AtLeastOnce)
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

        public MqttApplicationMessage ConstructWillMessage(string willPayload, bool retainFlag = true)
        {
            return ConstructMessage(topicD2C, "WILL message " + willPayload, retainFlag);
        }

        public async Task SendDeviceToCloudMessageAsync(string payload, bool retainFlag = false, string topicParameters = "")
        {
            var message = ConstructMessage(topicD2C + topicParameters, payload, retainFlag);

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

        public void DisconnectUngracefully()
        {
            mqttClient.Dispose();
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