// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace MessageSample
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var device = new Device();
            await device.RunSampleAsync();

            return 0;
        }
    }
}
