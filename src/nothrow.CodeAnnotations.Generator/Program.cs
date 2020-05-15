using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        private readonly CommandLineSettings _commandLineSettings;

        public Program(ILogger<Program> logger, CommandLineSettings commandLineSettings)
        {
            _logger = logger;
            _commandLineSettings = commandLineSettings;
        }

        private int Execute()
        {
            try
            {
                _commandLineSettings.AssertValid();
                if (_commandLineSettings.ShowHelp)
                    return ShowHelp();


                switch (_commandLineSettings.Task)
                {
                    case "scaffold":
                        return Scaffold();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            catch (CommandLineSettingsInvalidException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected failure");
                return 1;
            }

            return 0;
        }

        private int Scaffold()
        {
            if (string.IsNullOrEmpty(_commandLineSettings.InputAssembly))
                throw new CommandLineSettingsInvalidException("Missing -i parameter");
            if (string.IsNullOrEmpty(_commandLineSettings.ScaffoldingDirectory))
                throw new CommandLineSettingsInvalidException("Missing -s parameter");

            var input = Path.GetFullPath(_commandLineSettings.InputAssembly);
            var output = Path.GetFullPath(_commandLineSettings.ScaffoldingDirectory);

            _logger.LogInformation("Generating scaffold for assembly {assembly} to {scaffoldDir}", input, output);

            var asm = new AssemblyAnalyzer(input);
            var types = asm.Analyze();

            GenerateNamespaceDirectories(output, types);

            
            File.WriteAllText(Path.Combine(output, "typeinfo.json"), JsonSerializer.Serialize(types, new JsonSerializerOptions {WriteIndented = true}));

            return 0;
        }

        private void GenerateNamespaceDirectories(string path, NamespaceHierarchy ns)
        {
            var myPath = Path.Combine(path, ns.Name);
            var myNsInfoPath = Path.Combine(myPath, "_namespace.md");
            Directory.CreateDirectory(myPath);

            var fi = new FileInfo(myNsInfoPath);
            if (fi.Exists && fi.Length > 0)
            {
                _logger.LogInformation("There is already nonempty namespace.md for {namespace}, not generating rest");
                return;
            }

            _logger.LogInformation("Generating directory for namespace {namespace}", ns.Name);


            File.WriteAllText(myNsInfoPath, "");

            foreach (var sns in ns.Namespaces)
            {
                GenerateNamespaceDirectories(myPath, sns);
            }

            foreach (var type in ns.Types)
            {
                var safeTypeName = type.Name.Replace('<', '_').Replace('>', '_');

                var typeFile = Path.Combine(myPath, "T_" + safeTypeName + ".md");

                if (!File.Exists(typeFile))
                {
                    File.WriteAllText(typeFile, "");
                    _logger.LogInformation("Generating file for type {type}", type.Name);
                }
                else
                {
                    _logger.LogDebug("File for {type} already exists, skipping", type.Name);
                }
            }
        }

        private int ShowHelp()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("dotnet annotation -(h|?) - shows this help");
            Console.WriteLine();
            Console.WriteLine("dotnet annotation scaffold [-i assembly.dll] [-s scaffold_directory]");
            Console.WriteLine(" Scans the assembly.dll for all classes/namespaces, and generates");
            Console.WriteLine(" scaffolding for the annotations in output_directory.");
            Console.WriteLine("");
            Console.WriteLine(" -i assembly.dll       - REQUIRED      - name of the assembly to analyze");
            Console.WriteLine(" -s scaffold_directory - default 'out' - where to put the scaffolding");

            return 0;
        }

        static int Main(string[] args)
        {
            var configuration = BuildConfiguration(args);
            using var serviceProvider = BuildServices(configuration, args);

            var service = serviceProvider.GetService<Program>();
            return service.Execute();
        }

        private static ServiceProvider BuildServices(IConfigurationRoot configuration, string[] args)
        {
            var serviceBuilder = new ServiceCollection();
            serviceBuilder.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));

                logging.AddConsole();
                logging.AddDebug();
            });

            serviceBuilder.AddSingleton<Program>();
            serviceBuilder.AddSingleton<CommandLineSettings>(_ => new CommandLineSettings(args));

            var serviceProvider = serviceBuilder.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
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
