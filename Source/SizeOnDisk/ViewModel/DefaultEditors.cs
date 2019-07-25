using Microsoft.Win32;
using SizeOnDisk.Shell;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SizeOnDisk.ViewModel
{
    public static class DefaultEditors
    {
        private static IEnumerable<ShellCommandSoftware> list = null;
        private static readonly object _lock = new object();

        public static IEnumerable<ShellCommandSoftware> Editors
        {
            get
            {
                if (list == null)
                {
                    lock (_lock)
                    {
                        if (list == null)
                        {
                            List<ShellCommandSoftware> newlist = new List<ShellCommandSoftware>();
                            IEnumerable<DefaultEditorItem> editorlist = MainConfiguration.Instance?.Editors;
                            if (editorlist == null)
                            {
                                editorlist = new List<DefaultEditorItem>();
                            }
                            foreach (string mainType in editorlist.Select(T => T.Display).Distinct())
                            {
                                foreach (DefaultEditorItem item in editorlist.Where(T => T.Display == mainType))
                                {
                                    bool found = false;
                                    ShellCommandSoftware command = new ShellCommandSoftware
                                    {
                                        Id = item.Display
                                    };
                                    switch (item.Definition)
                                    {
                                        case DefaultEditorDefinitionType.ApplicationKey:
                                            string key = Registry.GetValue($@"HKEY_CLASSES_ROOT\Applications\{item.Parameter1}\shell\open\command", string.Empty, string.Empty)?.ToString();
                                            if (!string.IsNullOrEmpty(key))
                                            {
                                                command.Icon = ShellHelper.SafeNativeMethods.ExtractIconFromDLL(ShellHelper.SplitCommandAndParameters(key).Item1, 0);
                                                command.Name = key;
                                                found = true;
                                            }
                                            break;
                                        case DefaultEditorDefinitionType.SoftwareKey:
                                            string value = Registry.GetValue(item.Parameter1, string.Empty, string.Empty)?.ToString();
                                            if (!string.IsNullOrEmpty(value))
                                            {
                                                string path = string.Concat(value, "\\", item.Parameter2);
                                                command.Icon = ShellHelper.SafeNativeMethods.ExtractIconFromDLL(path, 0);
                                                command.Name = path;
                                                found = true;
                                            }
                                            break;
                                        case DefaultEditorDefinitionType.File:
                                            if (File.Exists(item.Parameter1))
                                            {
                                                command.Icon = ShellHelper.SafeNativeMethods.ExtractIconFromDLL(item.Parameter1, 0);
                                                command.Name = item.Parameter1;
                                                found = true;
                                            }
                                            break;
                                    }
                                    if (found)
                                    {
                                        newlist.Add(command);
                                        break;
                                    }
                                }
                            }
                            list = newlist;
                        }
                    }
                }
                return list;
            }
        }


    }
}
