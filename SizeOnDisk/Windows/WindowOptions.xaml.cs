﻿using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace SizeOnDisk.Windows
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    [SuppressMessage("Design", "CA1501")]
    public partial class WindowOptions : Window
    {
        public WindowOptions(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

    }
}
