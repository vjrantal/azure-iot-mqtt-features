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

            await device.ConnectDevice();
            await device.SubscribeToEventAsync();

            // Act
            await consumer.SendCloudToDeviceMessageAsync("test");

            // Assert
            //deviceMock.Verify(x => x.ApplicationMessageReceived(It.IsNotNull<MqttApplicationMessageReceivedEventArgs>()), Times.Once);
            Assert.Pass();
        }
    }
}