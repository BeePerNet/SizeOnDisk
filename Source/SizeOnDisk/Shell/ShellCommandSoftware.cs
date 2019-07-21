﻿using System.Collections.Generic;
using System.Windows.Media.Imaging;
using SizeOnDisk.Shell;

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