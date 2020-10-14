using System;
using System.Threading.Tasks;
using MQTTnet;

namespace MessageSample
{
    public interface IDevice
    {
        Task ConnectDevice();
        Task SendEventAsync();
        Task SubscribeToEventAsync();
        void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e);
    }
}
