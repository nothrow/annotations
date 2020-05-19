using System.Collections.Generic;

namespace code_annotations.Generator.Models
{
    internal class BrowserDatabase
    {
        public Dictionary<string, AnalyzedAssembly> Assemblies { get; set; }
        public string GeneratedOn { get; set; }
        public string Version { get; set; }
    }
}