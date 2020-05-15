using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace code_annotations.Generator
{
    class Program
    {
        private readonly ILogger<Program> _logger;

        public Program(ILogger<Program> logger)
        {
            _logger = logger;
        }

        private int Execute()
        {
            return 1;
        }

        static int Main(string[] args)
        {
            var configuration = BuildConfiguration(args);
            using var serviceProvider = BuildServices(configuration);

            var service = serviceProvider.GetService<Program>();
            return service.Execute();
        }

        private static ServiceProvider BuildServices(IConfigurationRoot configuration)
        {
            var serviceBuilder = new ServiceCollection();
            serviceBuilder.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));

                logging.AddConsole();
                logging.AddDebug();
            });

            serviceBuilder.AddSingleton<Program>();

            var serviceProvider = serviceBuilder.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true, ValidateScopes = true
            });
            return serviceProvider;
        }

        private static IConfigurationRoot BuildConfiguration(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configurationBuilder.AddEnvironmentVariables("DOTNET_");
            configurationBuilder.AddCommandLine(args);

            var configuration = configurationBuilder.Build();
            return configuration;
        }

    }
}
