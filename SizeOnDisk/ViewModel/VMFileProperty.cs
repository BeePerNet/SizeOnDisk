using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SizeOnDisk.ViewModel
{
    public class VMFileProperty
    {
        public VMFileProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; }

        public string Value { get; }
    }
}
