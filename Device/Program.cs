// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices.Client;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;

namespace MessageSample
{
    public class Program
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

        public static async Task<int> Main(string[] args)
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

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            await mqttClient.ConnectAsync(options, CancellationToken.None);

            mqttClient.UseDisconnectedHandler(async e =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await mqttClient.ConnectAsync(options, CancellationToken.None);
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("devices/" + deviceId + "/messages/events/")
                .WithPayload("Hello World")
                .WithAtLeastOnceQoS()
                .WithRetainFlag()
                .Build();

            await mqttClient.PublishAsync(message, CancellationToken.None);

            return 0;
        }

        private static string GenerateSasToken(string resourceUri, string key, int expiryInSeconds = 3600)
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
