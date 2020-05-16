using System.Collections.Generic;

namespace code_annotations.Generator
{
    internal class AnalyzedAssembly
    {
        public NamespaceHierarchy Namespaces { get; set; }
        public string AssemblyName { get; set; }
        public List<string> Strings { get; set; } = new List<string>();
    }

    internal class NamespaceHierarchy
    {
        public NamespaceHierarchy(){}

        public NamespaceHierarchy(string namespaceName, IReadOnlyCollection<NamespaceHierarchy> namespaces, IReadOnlyCollection<TypeInformation> types)
        {
            NamespaceName = namespaceName;
            Namespaces = namespaces;
            Types = types;
        }

        public string NamespaceName { get; set; }
        public int[] Comment { get; set; }
        public IReadOnlyCollection<NamespaceHierarchy> Namespaces { get; set; }
        public IReadOnlyCollection<TypeInformation> Types { get; set; }
    }
}