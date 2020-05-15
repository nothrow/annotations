using System.Collections.Generic;

namespace code_annotations.Generator
{
    internal class NamespaceHierarchy
    {
        public NamespaceHierarchy(string name, IReadOnlyCollection<NamespaceHierarchy> namespaces, IReadOnlyCollection<TypeInformation> types)
        {
            Name = name;
            Namespaces = namespaces;
            Types = types;
        }

        public string Name { get; }
        public IReadOnlyCollection<NamespaceHierarchy> Namespaces { get; }
        public IReadOnlyCollection<TypeInformation> Types { get; }
    }
}