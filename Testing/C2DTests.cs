using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting;
using IotHubConsumer;
using MessageSample;
using MQTTnet;
using MQTTnet.Protocol;
using NUnit.Framework;

namespace Testing
{
    public class C2DTests
    {
        private string iotHubConnectionString;
        private string iotHubDeviceConnectionString;
        private string deviceId;
        private string eventHubCompatibleEndpoint;
        private string eventHubName;
        private string iotHubSasKey;


        [SetUp]
        public void Setup()
        {
            var configuration = Configuration.BuildConfiguration();
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
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                payloads.Add(e.ApplicationMessage.ConvertPayloadToString());
            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

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

            await device.ConnectDevice();

            var retainFlag = true;
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, retainFlag);

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert - verify message was received + mqtt-retain set to true
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true");
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

            await device.ConnectDevice();
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                payloads.Add(e.ApplicationMessage.ConvertPayloadToString());
            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Act
            await sender.SendCloudToDeviceMessageAsync(firstPayload);
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == firstPayload) != null, TimeSpan.FromSeconds(10)));

            await device.DisconnectDevice();
            Thread.Sleep(15000);
            await sender.SendCloudToDeviceMessageAsync(secondPayload);

            device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice();
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Assert
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payloads.FirstOrDefault(x => x == secondPayload) != null, TimeSpan.FromSeconds(10)));
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

        public async Task ReceiveD2CMessageWithQosZero()
        {
            // Arrange
            var receiver = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice();
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, false);

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null);
        }
    }
}