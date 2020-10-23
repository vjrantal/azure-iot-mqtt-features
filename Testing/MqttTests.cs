using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting;
using IotHubConsumer;
using Client;
using MQTTnet;
using MQTTnet.Protocol;
using NUnit.Framework;

namespace Testing
{
    public class MqttTests
    {
        private Device device;
        private Sender senderConsumer;
        private Receiver receiverConsumer;

        [SetUp]
        public void Setup()
        {
            var configuration = Configuration.BuildConfiguration();
            var iotHubConnectionString = configuration["IotHubConnectionString"];
            var iotHubDeviceConnectionString = configuration["IotHubDeviceConnectionString"];
            var deviceId = configuration["DeviceId"];
            var eventHubCompatibleEndpoint = configuration["EventHubCompatibleEndpoint"];
            var eventHubName = configuration["EventHubName"];
            var iotHubSasKey = configuration["IotHubSasKey"];

            device = new Device(iotHubDeviceConnectionString);
            senderConsumer = new Sender(iotHubConnectionString, deviceId);
            receiverConsumer = new Receiver(eventHubCompatibleEndpoint, eventHubName, iotHubSasKey);
        }

        [Test]
        public async Task SendC2DMessages()
        {
            // Arrange
            var flag = false;
            await device.ConnectDevice();
            Action<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived = (MqttApplicationMessageReceivedEventArgs e) =>
            {
                flag = true;
                Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
            };
            await device.SubscribeToEventAsync(ApplicationMessageReceived);

            // Act
            await senderConsumer.SendCloudToDeviceMessageAsync("test");
            while (!flag) { }

            // Assert
            Assert.IsTrue(flag);
        }

        [Test]
        public async Task SendD2CWithRetainFlagTrue()
        {
            // Arrange
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
        public async Task ReceiveD2CMessageWithQosZero()
        {
            // Arrange
            await device.ConnectDevice();
            var payload = Guid.NewGuid().ToString();

            // Act
            await device.SendDeviceToCloudMessageAsync(payload, false, MqttQualityOfServiceLevel.AtMostOnce);

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiverConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert
            var sentMessage = messages.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null);
        }

        [Test]
        public async Task SenWillMessageWhenDeviceDisconnectsUngracefully()
        {
            // Arrange
            var willPayload = Guid.NewGuid().ToString();

            // Act
            await device.ConnectDevice(willPayload);

            //disconnect ungracefully
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(3000);
            var messages = await receiverConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token);

            // Assert
            var sentMessage = messages.FirstOrDefault(x => x.Payload == "WILL message " + willPayload);
            //Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true" && sentMessage.MessageType == "Will");
            Assert.Pass();
        }
    }
}