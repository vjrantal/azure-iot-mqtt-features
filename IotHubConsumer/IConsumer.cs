using System;
using System.Threading.Tasks;

namespace IotHubConsumer
{
    public interface IConsumer
    {
        Task SendCloudToDeviceMessageAsync(string message);
        void ConnectConsumer();
    }
}
