// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Threading.Tasks;
using CrossCutting;
using MQTTnet;

namespace MessageSample
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var configuration = Configuration.BuildConfiguration();
            var device = new Device(configuration);
            await device.RunSampleAsync(ApplicationMessageReceived);
            return 0;
        }

        public static void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"Got message: ClientId:{e.ClientId} Topic:{e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
        }
    }
}
