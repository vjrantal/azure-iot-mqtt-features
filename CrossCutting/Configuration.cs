using System;
using Microsoft.Extensions.Configuration;

namespace CrossCutting
{
    public class Configuration
    {
        public static IConfigurationRoot BuildConfiguration()
        {
            var basePath = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("/bin"));
            var projectRoot = AppContext.BaseDirectory.Substring(0, basePath.LastIndexOf('/'));
            return new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile("Properties/appsettings.json", optional: false)
                .Build();
        }
    }
}
