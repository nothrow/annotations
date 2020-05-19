using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using code_annotations.Generator.Models;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace code_annotations.Generator
{
    internal class Program
    {
        private readonly CommandLineSettings _commandLineSettings;
        private readonly ILogger<Program> _logger;

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
                {
                    return ShowHelp();
                }


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

        private static void DumpResourceFile(string path, string filename, string replace = null)
        {
            using StreamReader sr =
                new StreamReader(
                    typeof(Program).Assembly.GetManifestResourceStream(
                        $"code_annotations.Generator.content.{filename}"));
            string text = sr.ReadToEnd();
            if (replace != null)
            {
                text = text.Replace("%REPLACE%", replace);
            }

            File.WriteAllText(Path.Combine(path, filename), text);
        }

        private int Generate()
        {
            if (string.IsNullOrEmpty(_commandLineSettings.OutputDirectory))
            {
                throw new CommandLineSettingsInvalidException("Missing -o parameter");
            }

            if (string.IsNullOrEmpty(_commandLineSettings.AnnotationDirectory))
            {
                throw new CommandLineSettingsInvalidException("Missing -s parameter");
            }

            string input = Path.GetFullPath(_commandLineSettings.AnnotationDirectory);
            string output = Path.GetFullPath(_commandLineSettings.OutputDirectory);

            Directory.CreateDirectory(output);

            var assemblies = new Dictionary<string, AnalyzedAssembly>();

            foreach (string f in Directory.EnumerateFiles(input, "A_*.json"))
            {
                _logger.LogInformation("Loading assembly from {assembly}", f);
                AnalyzedAssembly asm = JsonSerializer.Deserialize<AnalyzedAssembly>(File.ReadAllText(f));
                ReadNamespaceInformation(asm, Path.Combine(input, asm.AssemblyName), asm.Namespaces,
                    ImmutableArray<int>.Empty, null);

                assemblies[asm.AssemblyName] = asm;
            }

            string dbcon = "window.annotationInfo = " + JsonSerializer.Serialize(new BrowserDatabase
            {
                Assemblies = assemblies,
                GeneratedOn = DateTime.Now.ToString(),
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            }) + ";";
            using SHA1 s = SHA1.Create();

            string hash = Convert.ToBase64String(s.ComputeHash(Encoding.UTF8.GetBytes(dbcon)));


            File.WriteAllText(Path.Combine(output, "db.js"), dbcon);


            _logger.LogInformation("Generating html/javascript browser");
            DumpResourceFile(output, "browser.js");
            DumpResourceFile(output, "README.md");
            DumpResourceFile(output, "index.html", hash);

            return 0;
        }

        private static readonly Regex _tagsRegex = new Regex(@"#(\w*([0-9a-zA-Z]|-)+\w*[0-9a-zA-Z])");
        private static TextWithTags ProcessContent(string filePath, string heading, int level)
        {
            StringBuilder s = new StringBuilder();
            string f = File.ReadAllText(filePath);



            switch (level)
            {
                case 1:
                    s.Append(new string('#', 3));
                    break;
                case 2:
                    s.Append(new string('#', 4));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            s.Append(' ');
            s.AppendLine(heading);

            s.AppendLine(f);
            return new TextWithTags
            {
                String = Markdown.ToHtml(s.ToString()),
                Tags = _tagsRegex.Matches(f).Select(m => m.Groups[0].Value).ToList()
            };
        }

        private void ReadNamespaceInformation(AnalyzedAssembly asm, string input,
            NamespaceHierarchy types,
            ImmutableArray<int> comments, string parent)
        {
            var fullNamespace = parent != null ? parent + "." + types.NamespaceName : types.NamespaceName;

            _logger.LogInformation("Processing {namespace}", fullNamespace);

            string path = Path.Combine(input, types.NamespaceName);

            FileInfo nsInfo = new FileInfo(Path.Combine(path, "_namespace.md"));
            if (nsInfo.Exists && nsInfo.Length > 0)
            {
                asm.Strings.Add(ProcessContent(nsInfo.FullName, "📂 Namespace " + fullNamespace, 1));

                comments = comments.Add(asm.Strings.Count - 1);
            }
            
            types.Comment = comments.ToArray();

            foreach (TypeInformation type in types.Types)
            {
                FileInfo typeInfo = new FileInfo(Path.Combine(path, GetFileNameForType(type.Name)));
                ImmutableArray<int> fcomments = comments;
                if (typeInfo.Exists && typeInfo.Length > 0)
                {
                    asm.Strings.Add(ProcessContent(typeInfo.FullName, "📦 Type " + fullNamespace + "." + type.Name, 2));

                    fcomments = fcomments.Add(asm.Strings.Count - 1);
                    
                }

                type.Comment = fcomments.ToArray();
            }

            foreach (NamespaceHierarchy ns in types.Namespaces)
            {
                ReadNamespaceInformation(asm, path, ns, comments, fullNamespace);
            }
        }

        private int Scaffold()
        {
            if (string.IsNullOrEmpty(_commandLineSettings.InputAssembly))
            {
                throw new CommandLineSettingsInvalidException("Missing -i parameter");
            }

            if (string.IsNullOrEmpty(_commandLineSettings.AnnotationDirectory))
            {
                throw new CommandLineSettingsInvalidException("Missing -s parameter");
            }

            string input = Path.GetFullPath(_commandLineSettings.InputAssembly);
            string output = Path.GetFullPath(_commandLineSettings.AnnotationDirectory);

            _logger.LogInformation("Generating scaffold for assembly {assembly} to {scaffoldDir}", input, output);

            AssemblyAnalyzer asm = new AssemblyAnalyzer(input);
            AnalyzedAssembly types = asm.Analyze();

            GenerateNamespaceDirectories(Path.Combine(output, types.AssemblyName), types.Namespaces);


            File.WriteAllText(Path.Combine(output, $"A_{types.AssemblyName}.json"),
                JsonSerializer.Serialize(types, new JsonSerializerOptions { WriteIndented = true }));
            DumpResourceFile(output, "README.md");

            return 0;
        }

        private static string GetFileNameForType(string type)
        {
            string safeTypeName = type.Replace('<', '_').Replace('>', '_');
            return "T_" + safeTypeName + ".md";
        }

        private void GenerateNamespaceDirectories(string path, NamespaceHierarchy ns)
        {
            string myPath = Path.Combine(path, ns.NamespaceName);
            string myNsInfoPath = Path.Combine(myPath, "_namespace.md");
            Directory.CreateDirectory(myPath);

            FileInfo fi = new FileInfo(myNsInfoPath);
            if (fi.Exists && fi.Length > 0)
            {
                _logger.LogInformation("There is already nonempty namespace.md for {namespace}, not generating rest");
                return;
            }

            _logger.LogInformation("Generating directory for namespace {namespace}", ns.NamespaceName);


            File.WriteAllText(myNsInfoPath, "");

            foreach (NamespaceHierarchy sns in ns.Namespaces)
            {
                GenerateNamespaceDirectories(myPath, sns);
            }

            foreach (TypeInformation type in ns.Types)
            {
                string typeFile = Path.Combine(myPath, GetFileNameForType(type.Name));

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
            Console.WriteLine(
                " -s annotation_directory - REQUIRED      - where to read the annotations from. previously generated by scaffold");
            Console.WriteLine(" -o output_directory     - REQUIRED      - where to put the output (html)");

            return 0;
        }

        private static int Main(string[] args)
        {
            IConfigurationRoot configuration = BuildConfiguration(args);
            using ServiceProvider serviceProvider = BuildServices(configuration, args);

            Program service = serviceProvider.GetService<Program>();
            return service.Execute();
        }

        private static ServiceProvider BuildServices(IConfigurationRoot configuration, string[] args)
        {
            ServiceCollection serviceBuilder = new ServiceCollection();
            serviceBuilder.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));

                logging.AddConsole();
                logging.AddDebug();
            });

            serviceBuilder.AddSingleton<Program>();
            serviceBuilder.AddSingleton(_ => new CommandLineSettings(args));

            ServiceProvider serviceProvider = serviceBuilder.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
            return serviceProvider;
        }

        private static IConfigurationRoot BuildConfiguration(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddJsonFile("appsettings.json", true, true);
            configurationBuilder.AddEnvironmentVariables("DOTNET_");
            configurationBuilder.AddCommandLine(args);

            IConfigurationRoot configuration = configurationBuilder.Build();
            return configuration;
        }
    }
}