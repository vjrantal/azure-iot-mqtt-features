using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting;
using IotHubConsumer;
using MessageSample;
using MQTTnet;
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

            var payload = Guid.NewGuid().ToString();
            var receivedPayload = string.Empty;

            await device.ConnectDevice();
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                receivedPayload = e.ApplicationMessage.ConvertPayloadToString();

            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Act
            await sender.SendCloudToDeviceMessageAsync(payload);

            // Assert
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payload == receivedPayload, TimeSpan.FromSeconds(10)));
        }

        [Test]
        public async Task SendD2CWithRetainFlagTrue()
        {
            // Arrange
            var receiverConsumer = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
            var device = new Device(iotHubDeviceConnectionString);

            await device.ConnectDevice();

            var retainFlag = true;
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, retainFlag);

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiverConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert - verify message was received + mqtt-retain set to true
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true");
        }

        [Test]
        public async Task ReceiveAllC2DMessagesWithCleanSessionFalse()
        {
            // Arrange
            var device = new Device(iotHubDeviceConnectionString);
            var sender = new Sender(iotHubConnectionString, deviceId);
            var firstPayload = Guid.NewGuid().ToString();
            var secondPayload = Guid.NewGuid().ToString();

            var payload = string.Empty;
            await device.ConnectDevice();
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                payload = e.ApplicationMessage.ConvertPayloadToString();
            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Act
            await sender.SendCloudToDeviceMessageAsync(firstPayload);
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payload == firstPayload, TimeSpan.FromSeconds(10)));

            await device.DisconnectDevice();
            Thread.Sleep(15000);
            await sender.SendCloudToDeviceMessageAsync(secondPayload);

            device = new Device(iotHubDeviceConnectionString);
            await device.ConnectDevice(false);
            await device.SubscribeToEventAsync(ApplicationMessageReceived);
            // await sender.SendCloudToDeviceMessageAsync(secondPayload);

            // Assert
            Assert.IsTrue(RetryUntilSuccessOrTimeout(() => payload == secondPayload, TimeSpan.FromSeconds(10)));
        }
        private bool RetryUntilSuccessOrTimeout(Func<bool> task, TimeSpan timeSpan)
        {
            var success = false;
            var elapsed = 0;
            while ((!success) && (elapsed < timeSpan.TotalMilliseconds))
            {
                Thread.Sleep(100);
                elapsed += 100;
                success = task();
            }
            return success;
        }

    }
}