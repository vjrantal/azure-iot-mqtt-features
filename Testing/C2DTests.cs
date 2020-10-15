using System;
using System.Threading.Tasks;
using CrossCutting;
using IotHubConsumer;
using MessageSample;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using System.IO;
using NUnit.Framework;

namespace Testing
{
    public class C2DTests
    {
        private Device device;
        private Consumer consumer;

        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            var dir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var configuration = Configuration.BuildConfiguration(Path.GetPathRoot(dir));
            device = new Device(configuration);
            consumer = new Consumer(configuration);
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
            while (!flag)
            {

            }
            Assert.IsTrue(flag);
            //Assert.Pass();
        }
    }
}