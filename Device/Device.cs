﻿using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
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
        private readonly IMqttClient mqttClient;
        private static string sharedAccessKey;
        private static string hubAddress;

        private string deviceId;

        public Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public Device(IConfiguration configuration)
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
            var connectionString = configuration["IotHubDeviceConnectionString"].Split(';');
            deviceId = connectionString[1].Split('=', 2)[1];
            sharedAccessKey = connectionString[2].Split('=', 2)[1];
            hubAddress = connectionString[0].Split('=', 2)[1];
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
                .WithTls()
                .Build();

            await mqttClient.ConnectAsync(options, CancellationToken.None);
            mqttClient.UseDisconnectedHandler(new MqttClientDisconnectedHandlerDelegate(e => Disconnected(e, options)));
        }

        public MqttApplicationMessage ConstructMessage(string topic, string payload, bool retainFlag = false )
        {
            /*var payloadJObject = new JObject
                {
                    { "OfficeTemperature", payload + DateTime.UtcNow.Millisecond.ToString() }
                };

            payload = JsonConvert.SerializeObject(payloadJObject);*/
            Console.WriteLine($"Topic:{topic} Payload:{payload}");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retainFlag)
                .WithAtLeastOnceQoS()
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

        public async Task SendD2CMessagesInALoopAsync() // TODO: delete if no longer used
        {
            var payload = 0;
            while (true)
            {
                await SendDeviceToCloudMessageAsync((payload++).ToString());
                Thread.Sleep(3000);
            }
        }

        public async Task SubscribeToEventAsync(Action<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived)
        {
            ApplicationMessageReceived = applicationMessageReceived;
            var topicC2D = $"devices/{deviceId}/messages/devicebound/#";

            mqttClient.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(e => ApplicationMessageReceived(e)));
            await mqttClient.SubscribeAsync(topicC2D, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
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
            Console.WriteLine("Reconnected");
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
