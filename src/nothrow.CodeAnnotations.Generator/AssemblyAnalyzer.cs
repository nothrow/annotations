using System;
using System.Collections.Generic;
using System.Linq;
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

        public NamespaceHierarchy Analyze()
        {
            using var asm = AssemblyDefinition.ReadAssembly(_input, new ReaderParameters(ReadingMode.Deferred));

            var types = new HashSet<TypeInformation>(TypeInformation.NameComparer);

            foreach (var module in asm.Modules)
                Analyze(module, types);

            var allNamespaces = types.Select(x => x.Namespace).Distinct().SelectMany(GeneratePath).Distinct().ToDictionary(x => x, _ => new List<TypeInformation>());

            foreach (var type in types)
            {
                allNamespaces[type.Namespace].Add(type);
            }

            var rootNamespaces = new List<NamespaceHierarchy>();
            var rootTypes = new List<TypeInformation>();

            BuildTree(allNamespaces, types, rootNamespaces, rootTypes, "");


            return new NamespaceHierarchy("", rootNamespaces, rootTypes);
        }

        private static string GetNamespaceLastPart(string ns)
        {
            return ns.Split('.').Last();
        }

        private static void BuildTree(IReadOnlyDictionary<string, List<TypeInformation>> allNamespaces,  IReadOnlyCollection<TypeInformation> types, List<NamespaceHierarchy> rootNamespaces, List<TypeInformation> rootTypes, string currentNs)
        {
            var subNses = allNamespaces.Keys.Where(x => IsDirectlyUnder(x, currentNs));

            foreach (var subNs in subNses)
            {
                var nh = new List<NamespaceHierarchy>();
                var ts = new List<TypeInformation>();

                BuildTree(allNamespaces, types, nh, ts, subNs);
                rootNamespaces.Add(new NamespaceHierarchy(GetNamespaceLastPart(subNs), nh, ts));
            }

            foreach(var type in types.Where(x => x.Namespace == currentNs))
            {
                rootTypes.Add(type);
            }
        }

        private static int Dots(string s)
        {
            if (string.IsNullOrEmpty(s))
                return -1;
            return s.Count(x => x == '.');
        }

        private static bool IsDirectlyUnder(string s, string currentNs)
        {
            if (s.StartsWith(currentNs))
            {
                return (Dots(s) == Dots(currentNs) + 1);
            }

            return false;
        }

        private IEnumerable<string> GeneratePath(string ns)
        {
            var split = ns.Split('.');
            var built = "";
            yield return "";
            foreach (var s in split)
            {
                if (built.Length == 0)
                    built = s;
                else
                {
                    built += "." + s;
                }

                yield return built;
            }
        }

        private static void Analyze(ModuleDefinition md, HashSet<TypeInformation> types)
        {
            foreach (var type in md.Types)
            {
                types.Add(new TypeInformation(type.Namespace, type.Name));
            }
        }
    }
}