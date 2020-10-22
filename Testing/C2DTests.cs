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
    }
}