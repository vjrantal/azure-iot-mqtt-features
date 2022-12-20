using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace CrossCutting
{
    public class Configuration
    {
        public static IConfigurationRoot BuildConfiguration()
        {
            var separator = Path.DirectorySeparatorChar;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var sourcePath = basePath.Substring(0, basePath.IndexOf(separator + "bin"));
            var projectRoot = basePath.Substring(0, sourcePath.LastIndexOf(separator));
            return new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile("Properties" + separator + "appsettings.json", optional: false)
                .Build();
        }
    }
}