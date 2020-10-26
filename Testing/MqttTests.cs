using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client;
using CrossCutting;
using IotHubConsumer;
using MQTTnet;
using MQTTnet.Protocol;
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
            var flag = false;
            var device = new Device(iotHubDeviceConnectionString);
            var sender = new Sender(iotHubConnectionString, deviceId);

            await device.ConnectDevice();
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                flag = true;
                Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Act
            await sender.SendCloudToDeviceMessageAsync("test");
            while (!flag) { }

            // Assert
            Assert.IsTrue(flag);
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

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiver.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert
            var sentMessage = messages.FirstOrDefault(x => x.Payload == "WILL message " + willPayload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true" && sentMessage.MessageType == "Will");
        }
    }
}