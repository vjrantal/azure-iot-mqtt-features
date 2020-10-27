using System;
using System.Collections.Concurrent;
using System.Linq;
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
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true");
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
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null);
        }

        [Test]
        public async Task ReceiveAllC2DMessagesWhileDisconnected()
        {
            // Arrange
            var device = new Device(iotHubDeviceConnectionString);
            var sender = new Sender(iotHubConnectionString, deviceId);
            var firstPayload = Guid.NewGuid().ToString();
            var secondPayload = Guid.NewGuid().ToString();
            var payloads = new ConcurrentBag<string>();
            Action<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                payloads.Add(e.ApplicationMessage.ConvertPayloadToString());
            };

            // Act
            await device.ConnectDevice();
            await device.SubscribeToEventAsync(applicationMessageReceived);
            await sender.SendCloudToDeviceMessageAsync(firstPayload);
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == firstPayload) != null, TimeSpan.FromSeconds(10)));

            await device.DisconnectDevice();
            await sender.SendCloudToDeviceMessageAsync(secondPayload);

            device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice();
            await device.SubscribeToEventAsync(applicationMessageReceived);

            // Assert
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
            var sentMessage = messages.FirstOrDefault(x => x.Payload == "WILL message " + willPayload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true" && sentMessage.MessageType == "Will");
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
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null);
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