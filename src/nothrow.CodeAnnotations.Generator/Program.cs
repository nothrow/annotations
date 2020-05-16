using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
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
                if (_commandLineSettings.ShowHelp || string.IsNullOrEmpty(_commandLineSettings.Task))
                    return ShowHelp();


                switch (_commandLineSettings.Task)
                {
                    case "scaffold":
                        return Scaffold();
                    case "generate":
                        return Generate();
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

        private int Generate()
        {
            if (string.IsNullOrEmpty(_commandLineSettings.OutputDirectory))
                throw new CommandLineSettingsInvalidException("Missing -o parameter");
            if (string.IsNullOrEmpty(_commandLineSettings.AnnotationDirectory))
                throw new CommandLineSettingsInvalidException("Missing -s parameter");

            var input = Path.GetFullPath(_commandLineSettings.AnnotationDirectory);
            var output = Path.GetFullPath(_commandLineSettings.OutputDirectory);

            Directory.CreateDirectory(output);

            var assemblies = new Dictionary<string, AnalyzedAssembly>();
            foreach (var f in Directory.EnumerateFiles(input, "A_*.json"))
            {
                _logger.LogInformation("Loading assembly from {assembly}", f);
                var asm = JsonSerializer.Deserialize<AnalyzedAssembly>(File.ReadAllText(f));
                ReadNamespaceInformation(asm, Path.Combine(input, asm.AssemblyName), asm.Namespaces, ImmutableArray<int>.Empty);

                assemblies[asm.AssemblyName] = asm;
            }

            File.WriteAllText(Path.Combine(output, "db.js"), "window.annotationInfo = " + JsonSerializer.Serialize(assemblies) + ";");

            return 0;
        }

        private static string ProcessContent(string filePath, string heading, int level)
        {
            var s = new StringBuilder();
            var f = File.ReadAllText(filePath);
            s.AppendLine(heading);
            switch (level)
            {
                case 1:
                    s.AppendLine(new string('=', heading.Length));
                    break;
                case 2:
                    s.AppendLine(new string('-', heading.Length));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            s.AppendLine(f);
            return Markdown.ToHtml(s.ToString());
        }

        private void ReadNamespaceInformation(AnalyzedAssembly asm, string input, NamespaceHierarchy types,
            ImmutableArray<int> comments)
        {
            _logger.LogInformation("Processing {namespace}", types.NamespaceName);

            var path = Path.Combine(input, types.NamespaceName);

            var nsInfo = new FileInfo(Path.Combine(path, "_namespace.md"));
            if (nsInfo.Exists && nsInfo.Length > 0)
            {
                asm.Strings.Add(ProcessContent(nsInfo.FullName, "📦 Namespace " + types.NamespaceName, 1));

                comments = comments.Add(asm.Strings.Count - 1);
            }

            types.Comment = comments.ToArray();

            foreach (var type in types.Types)
            {
                var typeInfo = new FileInfo(Path.Combine(path, GetFileNameForType(type.Name)));
                var fcomments = comments;
                if (typeInfo.Exists && typeInfo.Length > 0)
                {
                    asm.Strings.Add(ProcessContent(typeInfo.FullName, "⃣ Type " + type.Name, 2));

                    fcomments = fcomments.Add(asm.Strings.Count - 1);
                }

                type.Comment = fcomments.ToArray();
            }

            foreach (var ns in types.Namespaces)
            {
                ReadNamespaceInformation(asm, path, ns, comments);
            }
        }

        private int Scaffold()
        {
            if (string.IsNullOrEmpty(_commandLineSettings.InputAssembly))
                throw new CommandLineSettingsInvalidException("Missing -i parameter");
            if (string.IsNullOrEmpty(_commandLineSettings.AnnotationDirectory))
                throw new CommandLineSettingsInvalidException("Missing -s parameter");

            var input = Path.GetFullPath(_commandLineSettings.InputAssembly);
            var output = Path.GetFullPath(_commandLineSettings.AnnotationDirectory);

            _logger.LogInformation("Generating scaffold for assembly {assembly} to {scaffoldDir}", input, output);

            var asm = new AssemblyAnalyzer(input);
            var types = asm.Analyze();

            GenerateNamespaceDirectories(Path.Combine(output, types.AssemblyName), types.Namespaces);


            File.WriteAllText(Path.Combine(output, $"A_{types.AssemblyName}.json"), JsonSerializer.Serialize(types, new JsonSerializerOptions { WriteIndented = true }));

            return 0;
        }

        private static string GetFileNameForType(string type)
        {
            var safeTypeName = type.Replace('<', '_').Replace('>', '_');
            return "T_" + safeTypeName + ".md";
        }

        private void GenerateNamespaceDirectories(string path, NamespaceHierarchy ns)
        {
            var myPath = Path.Combine(path, ns.NamespaceName);
            var myNsInfoPath = Path.Combine(myPath, "_namespace.md");
            Directory.CreateDirectory(myPath);

            var fi = new FileInfo(myNsInfoPath);
            if (fi.Exists && fi.Length > 0)
            {
                _logger.LogInformation("There is already nonempty namespace.md for {namespace}, not generating rest");
                return;
            }

            _logger.LogInformation("Generating directory for namespace {namespace}", ns.NamespaceName);


            File.WriteAllText(myNsInfoPath, "");

            foreach (var sns in ns.Namespaces)
            {
                GenerateNamespaceDirectories(myPath, sns);
            }

            foreach (var type in ns.Types)
            {
                var typeFile = Path.Combine(myPath, GetFileNameForType(type.Name));

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
            Console.WriteLine("dotnet annotation scaffold [-i assembly.dll] [-s annotation_directory]");
            Console.WriteLine(" Scans the assembly.dll for all classes/namespaces, and generates");
            Console.WriteLine(" scaffolding for the annotations in output_directory.");
            Console.WriteLine("");
            Console.WriteLine(" -i assembly.dll         - REQUIRED      - name of the assembly to analyze");
            Console.WriteLine(" -s annotation_directory - default 'out' - where to put the scaffolding");
            Console.WriteLine("");
            Console.WriteLine("dotnet annotation generate [-s annotation_directory] [-o output_directory]");
            Console.WriteLine(" Scans the assembly.dll for all classes/namespaces, and generates");
            Console.WriteLine(" scaffolding for the annotations in output_directory.");
            Console.WriteLine("");
            Console.WriteLine(" -s annotation_directory - REQUIRED      - where to read the annotations from. previously generated by scaffold");
            Console.WriteLine(" -o output_directory     - REQUIRED      - where to put the output (html)");

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
