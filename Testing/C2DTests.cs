using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossCutting;
using IotHubConsumer;
using MessageSample;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Testing
{
    public class C2DTests
    {
        private Device device;
        private SenderConsumer consumer;
        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            configuration = Configuration.BuildConfiguration();
            device = new Device(configuration);
            consumer = new SenderConsumer(configuration);
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
            consumer.ConnectConsumer();
            await consumer.SendCloudToDeviceMessageAsync("test");
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
            // send D2C with retain flag set to true
            await device.SendDeviceToCloudMessageAsync(payload, retainFlag);

            using var cancellationSource = new CancellationTokenSource();

            // Assert - verify received + mqtt-retain set to true
            cancellationSource.CancelAfter(3000);
            var recConsumer = new ReceiverConsumer(configuration);
            recConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token).Wait();

            var sentMessage = recConsumer.MessagesWithRetainSet.FirstOrDefault(x => x.Payload == payload);
            Assert.IsTrue(sentMessage != null && sentMessage.RetainFlag == "true"); // TODO: check if we can remove the retain flag assertion
        }
    }
}