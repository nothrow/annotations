using System.Collections.Generic;

namespace code_annotations.Generator.Models
{
    internal class TextWithTags
    {
        public string String { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}