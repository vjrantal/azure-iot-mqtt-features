using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Client;
using CrossCutting;
using IotHubConsumer;
using MQTTnet;
using NUnit.Framework;

namespace Testing
{
    public class MqttTests
    {
        private string iotHubConnectionString;
        private string iotHubDeviceConnectionString;
        private string iotHubDeviceCertConnectionString;
        private string iotHubDeviceSelfSignedCertConnectionString;
        private string deviceId;
        private string eventHubCompatibleEndpoint;
        private string eventHubName;
        private string iotHubSasKey;
        private string customEventHubCompatibleEndpoint;
        private string customEventHubName;

        [SetUp]
        public void Setup()
        {
            var configuration = Configuration.BuildConfiguration();
            customEventHubCompatibleEndpoint = configuration["CustomEventHubCompatibleEndpoint"];
            customEventHubName = configuration["CustomEventHubName"];
            iotHubConnectionString = configuration["IotHubConnectionString"];
            iotHubDeviceConnectionString = configuration["IotHubDeviceConnectionString"];
            iotHubDeviceSelfSignedCertConnectionString = configuration["IotHubDeviceSelfSignedCertConnectionString"];
            iotHubDeviceCertConnectionString = configuration["IotHubDeviceCertConnectionString"];
            deviceId = configuration["DeviceId"];
            eventHubCompatibleEndpoint = configuration["EventHubCompatibleEndpoint"];
            eventHubName = configuration["EventHubName"];
            iotHubSasKey = configuration["IotHubSasKey"];
        }

        [Test]
        public async Task SendC2DMessages()
        {
            // Arrange
            var device = new Device(iotHubDeviceConnectionString);
            var sender = new Sender(iotHubConnectionString, deviceId);
            var payloads = new ConcurrentBag<string>();
            var payload = Guid.NewGuid().ToString();
            await device.ConnectDevice();
            await device.SubscribeToEventAsync((MqttApplicationMessageReceivedEventArgs e) =>
            {
                payloads.Add(e.ApplicationMessage.ConvertPayloadToString());
            });

            // Act
            await sender.SendCloudToDeviceMessageAsync(payload);

            // Assert
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == payload) != null, TimeSpan.FromSeconds(10)));
        }

        [Test]
        public async Task SendD2CWithRetainFlagTrue()
        {
            // Arrange
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceConnectionString);
            var payload = Guid.NewGuid().ToString();

            await device.ConnectDevice();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, true);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert - verify message was received + mqtt-retain set to true
            Assert.IsTrue(messages.TryGetValue(payload, out var messageProperties));
            Assert.IsTrue(messageProperties.TryGetValue("mqtt-retain", out var retain) && retain.ToString() == "true");
        }

        [Test]
        public async Task ReceiveD2CMessageWithQosZero()
        {
            // Arrange
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice();
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, false);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert
            Assert.IsTrue(messages.ContainsKey(payload));
        }

        [Test]
        public async Task ReceiveAllC2DMessagesWhileDisconnected()
        {
            // Arrange
            var device = new Device(iotHubDeviceConnectionString);
            var sender = new Sender(iotHubConnectionString, deviceId);
            var payloads = new ConcurrentBag<string>();
            Action<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                payloads.Add(e.ApplicationMessage.ConvertPayloadToString());
            };

            // Act & Assert
            // Send first payload when device is connected 
            var firstPayload = Guid.NewGuid().ToString();
            await device.ConnectDevice();
            await device.SubscribeToEventAsync(applicationMessageReceived);
            await sender.SendCloudToDeviceMessageAsync(firstPayload);

            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == firstPayload) != null, TimeSpan.FromSeconds(10)));

            await device.DisconnectDevice();

            // Send second payload when device is disconnected 
            var secondPayload = Guid.NewGuid().ToString();
            await sender.SendCloudToDeviceMessageAsync(secondPayload);
            await device.ConnectDevice();
            await device.SubscribeToEventAsync(applicationMessageReceived);

            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == secondPayload) != null, TimeSpan.FromSeconds(10)));
        }

        [Test]
        public async Task SendWillMessageWhenDeviceDisconnectsUngracefully()
        {
            // Arrange
            var willPayload = Guid.NewGuid().ToString();
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceConnectionString);

            // Act
            await device.ConnectDevice(willPayload);
            device.DisconnectUngracefully();
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert
            Assert.IsTrue(messages.TryGetValue("WILL message " + willPayload, out var messageProperties));
            Assert.IsTrue(messageProperties.TryGetValue("mqtt-retain", out var retain) && retain.ToString() == "true");
            Assert.IsTrue(messageProperties.TryGetValue("iothub-MessageType", out var messageType) && messageType.ToString() == "Will");
        }

        [Test]
        public async Task ReceiveRoutedMessage()
        {
            // Arrange
            var receiver = new Receiver(customEventHubCompatibleEndpoint, customEventHubName);
            var device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice();
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, false, "&topic=status");
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert
            Assert.IsTrue(messages.TryGetValue(payload, out var messageProperties));
            Assert.IsTrue(messageProperties.TryGetValue("topic", out var value) && value.ToString() == "status");
        }

        [Test]
        public async Task DeviceCanConnectUsingCACertificate()
        {
            // Arrange
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceCertConnectionString);
            var payload = Guid.NewGuid().ToString();
            await device.ConnectDeviceUsingCertificate(new X509Certificate2("Certificates/CA-Certificate.pfx", "1234"));

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, true);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert 
            Assert.IsTrue(messages.ContainsKey(payload));
        }

        [Test]
        public async Task DeviceCanConnectUsingSelfSignedCertificate()
        {
            // Arrange
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceSelfSignedCertConnectionString);
            var payload = Guid.NewGuid().ToString();
            await device.ConnectDeviceUsingCertificate(new X509Certificate2("Certificates/SelfSigned-Certificate.pfx", "1234"));

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, true);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(new CancellationTokenSource(3000).Token);

            // Assert 
            Assert.IsTrue(messages.ContainsKey(payload));
        }

        private bool RetryUntilSuccessOrTimeout(Func<bool> task, TimeSpan timeSpan)
        {
            var success = false;
            var start = DateTime.Now;

            while ((!success) && DateTime.Now.Subtract(start).Seconds < timeSpan.Seconds)
            {
                Thread.Sleep(100);
                success = task();
            }
            return success;
        }
    }
}