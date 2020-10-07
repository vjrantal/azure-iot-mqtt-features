// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace MessageSample
{
    public class TelemetryMessage
    {
        private readonly DeviceClient deviceClient;

        public TelemetryMessage(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            await SendEventAsync();
            await ReceiveMessagesAsync();
        }

        private async Task SendEventAsync()
        {
            const int MessageCount = 5;
            Console.WriteLine($"Device sending {MessageCount} messages to IoT Hub...\n");

            var temperature  = 0;
            var humidity = 0;

            for (var count = 0; count < MessageCount; count++)
            {
                temperature++;
                humidity++;

                string dataBuffer = $"{{\"messageId\":{count},\"temperature\":{temperature},\"humidity\":{humidity}}}";

                using var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer))
                {
                    ContentType = "application/json",
                    ContentEncoding = Encoding.UTF8.ToString(),
                };

                const int TemperatureThreshold = 30;
                bool tempAlert = temperature > TemperatureThreshold;
                eventMessage.Properties.Add("temperatureAlert", tempAlert.ToString());
                Console.WriteLine($"\t{DateTime.Now}> Sending message: {count}, data: [{dataBuffer}]");

                await deviceClient.SendEventAsync(eventMessage);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            Console.WriteLine("\nDevice waiting for C2D messages from the hub...");
            Console.WriteLine("Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");

            using Message receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30));
            if (receivedMessage == null)
            {
                Console.WriteLine($"\t{DateTime.Now}> Timed out");
                return;
            }

            var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t{DateTime.Now}> Received message: {messageData}");

            var propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
            }

            await deviceClient.CompleteAsync(receivedMessage);
        }
    }
}
