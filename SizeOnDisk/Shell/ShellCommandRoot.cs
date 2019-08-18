using System.Collections.Generic;

namespace SizeOnDisk.Shell
{
    public class ShellCommandRoot
    {
        public string ContentType { get; set; }
        public string PerceivedType { get; set; }
        public ShellCommandSoftware Default { get; set; }
        public IList<ShellCommandSoftware> Softwares { get; } = new List<ShellCommandSoftware>();
    }
}
