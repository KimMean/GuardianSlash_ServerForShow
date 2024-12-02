using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Utilities
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ProductStringAttribute : Attribute
    {
        public string Name { get; }

        public ProductStringAttribute(string name)
        {
            Name = name;
        }
    }
}
