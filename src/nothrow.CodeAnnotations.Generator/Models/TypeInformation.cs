using System.Collections.Generic;

namespace code_annotations.Generator.Models
{
    internal class TypeInformation
    {
        public TypeInformation()
        {

        }
        public TypeInformation(string ns, string name)
        {
            Namespace = ns;
            Name = name;
        }

        public string Namespace { get; set; }
        public string Name { get; set; }
        public int[] Comment { get; set; }

        private sealed class NameEqualityComparer : IEqualityComparer<TypeInformation>
        {
            public bool Equals(TypeInformation x, TypeInformation y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(TypeInformation obj)
            {
                return (obj.Name != null ? obj.Name.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<TypeInformation> NameComparer { get; } = new NameEqualityComparer();
    }
}