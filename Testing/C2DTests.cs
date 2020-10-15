using System;
using System.Threading.Tasks;
using IotHubConsumer;
using MessageSample;
using Moq;
using MQTTnet;
using NUnit.Framework;

namespace Testing
{
    public class C2DTests
    {
        private Device device;
        private Consumer consumer;

        [SetUp]
        public void Setup()
        {
            device = new Device();
            consumer = new Consumer();

        }

        [Test]
        public async Task SendC2DMessages()
        {
            // Arrange
            var deviceMock = new Mock<IDevice>();
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
            // Assert
            //deviceMock.Verify(x => x.ApplicationMessageReceived(It.IsNotNull<MqttApplicationMessageReceivedEventArgs>()), Times.Once);
            Assert.IsTrue(flag);
            //Assert.Pass();
        }
    }
}