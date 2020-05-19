using System.Collections.Generic;
using System.Linq;
using code_annotations.Generator.Models;
using Mono.Cecil;

namespace code_annotations.Generator
{
    internal class AssemblyAnalyzer
    {
        private readonly string _input;

        public AssemblyAnalyzer(string input)
        {
            _input = input;
        }

        public AnalyzedAssembly Analyze()
        {
            using AssemblyDefinition asm =
                AssemblyDefinition.ReadAssembly(_input, new ReaderParameters(ReadingMode.Deferred));

            HashSet<TypeInformation> types = new HashSet<TypeInformation>(TypeInformation.NameComparer);

            foreach (ModuleDefinition module in asm.Modules)
            {
                Analyze(module, types);
            }

            Dictionary<string, List<TypeInformation>> allNamespaces = types.Select(x => x.Namespace).Distinct()
                .SelectMany(GeneratePath).Distinct().ToDictionary(x => x, _ => new List<TypeInformation>());

            foreach (TypeInformation type in types)
            {
                allNamespaces[type.Namespace].Add(type);
            }

            List<NamespaceHierarchy> rootNamespaces = new List<NamespaceHierarchy>();
            List<TypeInformation> rootTypes = new List<TypeInformation>();

            BuildTree(allNamespaces, types, rootNamespaces, rootTypes, "");


            return new
                AnalyzedAssembly
                {
                    Namespaces = new NamespaceHierarchy("", rootNamespaces, rootTypes), AssemblyName = asm.Name.Name
                };
        }

        private static string GetNamespaceLastPart(string ns)
        {
            return ns.Split('.').Last();
        }

        private static void BuildTree(IReadOnlyDictionary<string, List<TypeInformation>> allNamespaces,
            IReadOnlyCollection<TypeInformation> types, List<NamespaceHierarchy> rootNamespaces,
            List<TypeInformation> rootTypes, string currentNs)
        {
            IEnumerable<string> subNses = allNamespaces.Keys.Where(x => IsDirectlyUnder(x, currentNs));

            foreach (string subNs in subNses)
            {
                List<NamespaceHierarchy> nh = new List<NamespaceHierarchy>();
                List<TypeInformation> ts = new List<TypeInformation>();

                BuildTree(allNamespaces, types, nh, ts, subNs);
                rootNamespaces.Add(new NamespaceHierarchy(GetNamespaceLastPart(subNs), nh, ts));
            }

            foreach (TypeInformation type in types.Where(x => x.Namespace == currentNs))
            {
                rootTypes.Add(type);
            }
        }

        private static int Dots(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return -1;
            }

            return s.Count(x => x == '.');
        }

        private static bool IsDirectlyUnder(string s, string currentNs)
        {
            if (s.StartsWith(currentNs))
            {
                return Dots(s) == Dots(currentNs) + 1;
            }

            return false;
        }

        private static IEnumerable<string> GeneratePath(string ns)
        {
            string[] split = ns.Split('.');
            string built = "";
            yield return "";
            foreach (string s in split)
            {
                if (built.Length == 0)
                {
                    built = s;
                }
                else
                {
                    built += "." + s;
                }

                yield return built;
            }
        }

        private static void Analyze(ModuleDefinition md, ISet<TypeInformation> types)
        {
            foreach (TypeDefinition type in md.Types)
            {
                types.Add(new TypeInformation(type.Namespace, type.Name));
            }
        }
    }
}