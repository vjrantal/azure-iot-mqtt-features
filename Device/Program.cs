// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client;

namespace MessageSample
{
    public class Program
    {
        // String containing Hostname, Device Id, Module Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_MODULE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string connectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");

        public static int Main(string[] args)
        {
            var simDeviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            var sample = new Device(simDeviceClient);
            sample.RunSampleAsync().GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
