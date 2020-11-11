using System;
using Microsoft.Extensions.Configuration;

namespace CrossCutting
{
    public class Configuration
    {
        public static IConfigurationRoot BuildConfiguration()
        {
            var basePath = new Uri(AppDomain.CurrentDomain.BaseDirectory).AbsolutePath;
            var sourcePath = basePath.Substring(0, basePath.IndexOf("/bin"));
            var projectRoot = basePath.Substring(0, sourcePath.LastIndexOf('/'));
            return new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile("Properties/appsettings.json", optional: false)
                .Build();
        }
    }
}
