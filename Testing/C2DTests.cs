using System;
using System.IO;
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
            while (!flag) {}

            // Assert
            Assert.IsTrue(flag);
        }

        [Test]
        public async Task SendD2CWithRetainFlagTrue()
        {
            // Arrange
            await device.ConnectDevice();

            var retainFlag = true;
            var payloadJObject = new JObject
            {
                { "OfficeTemperature", "22." + DateTime.UtcNow.Millisecond.ToString() },
                { "OfficeHumidity", (DateTime.UtcNow.Second + 40).ToString() }
            };
            var payload = JsonConvert.SerializeObject(payloadJObject);

            // Act
            // send D2C with retain flag set to true
            await device.SendDeviceToCloudMessageAsync(payload, retainFlag);

            using var cancellationSource = new CancellationTokenSource();

            void cancelKeyPressHandler(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
                Console.WriteLine("Exiting...");

                Console.CancelKeyPress -= cancelKeyPressHandler;
            }

            Console.CancelKeyPress += cancelKeyPressHandler;

            // Assert - verify received
            var recConsumer = new ReceiverConsumer(configuration);
            recConsumer.ReceiveMessagesFromDeviceAsync(cancellationSource.Token).Wait();
            Assert.Pass();
        }
    }
}