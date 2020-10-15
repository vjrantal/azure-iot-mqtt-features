using System;
using System.Threading.Tasks;
using MQTTnet;

namespace MessageSample
{
    public interface IDevice
    {
        Task ConnectDevice();
        Task SendEventsAsync();
        Task SubscribeToEventAsync(Action<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived);
        Task SendCloudToDeviceMessageAsync(string payload);
        //void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e);
    }
}
