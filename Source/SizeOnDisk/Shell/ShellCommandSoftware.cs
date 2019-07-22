using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace SizeOnDisk.Shell
{
    public class ShellCommandSoftware
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public BitmapSource Icon { get; set; }
        public ShellCommandVerb Default { get; set; }
        public IList<ShellCommandVerb> Verbs { get; } = new List<ShellCommandVerb>();
    }
}
