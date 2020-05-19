using System.Collections.Generic;

namespace code_annotations.Generator.Models
{
    internal class AnalyzedAssembly
    {
        public NamespaceHierarchy Namespaces { get; set; }
        public string AssemblyName { get; set; }
        public List<TextWithTags> Strings { get; set; } = new List<TextWithTags>();
    }
}