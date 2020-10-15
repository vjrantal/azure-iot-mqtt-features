using System;
using Microsoft.Extensions.Configuration;

namespace CrossCutting
{
    public class Configuration
    {
        public static IConfigurationRoot BuildConfiguration(string basePath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }
    }
}
