using Microsoft.Win32;
using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.ViewModel
{
    public static class DefaultEditors
    {
        private enum FileType
        {
            Text, Binary
        }
        private enum ExecutableType
        {
            SoftwareKey,
            File
        }


        private static readonly List<Tuple<FileType, ExecutableType, string, string>> editors = new List<Tuple<FileType, ExecutableType, string, string>>();

        static DefaultEditors()
        {
            editors.Add(new Tuple<FileType, ExecutableType, string, string>(FileType.Text, ExecutableType.SoftwareKey, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Notepad++", "notepad++.exe"));
            editors.Add(new Tuple<FileType, ExecutableType, string, string>(FileType.Text, ExecutableType.SoftwareKey, @"HKEY_LOCAL_MACHINE\SOFTWARE\Notepad++", "notepad++.exe"));
            editors.Add(new Tuple<FileType, ExecutableType, string, string>(FileType.Text, ExecutableType.File, @"%SystemRoot%\system32\NOTEPAD.EXE", string.Empty));
        }

        private static bool IsInitialized = false;

        public static void InitializeDefaultHandlers(RoutedCommandEx textCommand)
        {
            if (!IsInitialized)
            {
                bool found = false;
                foreach (Tuple<FileType, ExecutableType, string, string> item in editors.Where(T => T.Item1 == FileType.Text))
                {
                    switch (item.Item2)
                    {
                        case ExecutableType.SoftwareKey:
                            string value = Registry.GetValue(item.Item3, string.Empty, string.Empty).ToString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                string path = string.Concat(value, "\\", item.Item4);
                                textCommand.Icon = new Image()
                                {
                                    Source = ShellHelper.SafeNativeMethods.ExtractIconFromDLL(path, 0),
                                    Width = 16,
                                    Height = 16
                                };
                                textCommand.Tag = "cmd:" + path;
                                found = true;
                            }
                            break;
                        case ExecutableType.File:
                            if (File.Exists(item.Item3))
                            {
                                textCommand.Icon = new Image()
                                {
                                    Source = ShellHelper.SafeNativeMethods.ExtractIconFromDLL(item.Item3, 0),
                                    Width = 16,
                                    Height = 16
                                };
                                textCommand.Tag = "cmd:" + item.Item3;
                                found = true;
                            }
                            break;
                    }
                    if (found) break;
                }
                IsInitialized = true;
            }
        }

    }
}
