using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WPFByYourCommand;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Extensions;
using WPFByYourCommand.Observables;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject
    {
        private readonly VMFile _vmFile;

        private static BitmapImage defaultFileBigIcon;
        private static BitmapImage GetDefaultFileBigIcon()
        {
            if (defaultFileBigIcon == null)
            {
                defaultFileBigIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileBig.png");
            }

            return defaultFileBigIcon;
        }

        private static BitmapImage defaultFileIcon;
        private static BitmapImage GetDefaultFileIcon()
        {
            if (defaultFileIcon == null)
            {
                defaultFileIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileSmall.png");
            }

            return defaultFileIcon;
        }

        private static BitmapImage defaultFolderBigIcon;
        private static BitmapImage GetDefaultFolderBigIcon()
        {
            if (defaultFolderBigIcon == null)
            {
                defaultFolderBigIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderBig.png");
            }

            return defaultFolderBigIcon;
        }

        private static BitmapImage defaultFolderIcon;
        private static BitmapImage GetDefaultFolderIcon()
        {
            if (defaultFolderIcon == null)
            {
                defaultFolderIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderSmall.png");
            }

            return defaultFolderIcon;
        }


        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        public LittleFileInfo Load()
        {
            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent.Path, _vmFile.Name);
            CreationTime = fileInfo.CreationTime;
            LastAccessTime = fileInfo.LastAccessTime;
            LastWriteTime = fileInfo.LastWriteTime;

            thumbnailInitialized = false;
            OnPropertyChanged(nameof(Thumbnail));

            return fileInfo;
        }

        public string FileType
        {
            get
            {
                if (!_vmFile.Root.IsDesign && _vmFile.IsFile)
                {
                    return ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public BitmapSource Icon
        {
            get
            {
                BitmapSource icon = null;
                try
                {
                    if (!_vmFile.Root.IsDesign)
                    {
                        icon = ShellHelper.GetIcon(_vmFile.Path, 16);
                    }

                    if (icon == null)
                    {
                        if (_vmFile.IsFile)
                        {
                            icon = GetDefaultFileIcon();
                        }
                        else
                        {
                            icon = GetDefaultFolderIcon();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _vmFile.LogException(ex);
                }
                return icon;
            }
        }


        private bool thumbnailInitialized = false;

        //Seems to have problems with VOB
        private BitmapSource _Thumbnail = null;
        public BitmapSource Thumbnail
        {
            get
            {
                try
                {
                    if (!_vmFile.Root.IsDesign)
                    {
                        if (_Thumbnail == null)
                            _Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);

                        if (!thumbnailInitialized)
                        {
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    BitmapSource thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                                    if (thumbnail == null)
                                    {
                                        if (_vmFile.IsFile)
                                        {
                                            thumbnail = GetDefaultFileBigIcon();
                                        }
                                        else
                                        {
                                            thumbnail = GetDefaultFolderBigIcon();
                                        }
                                    }
                                    if (!thumbnail.IsEqual(_Thumbnail))
                                    {
                                        _Thumbnail = thumbnail;
                                        OnPropertyChanged(nameof(Thumbnail));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _vmFile.LogException(ex);
                                }
                            }, TaskCreationOptions.LongRunning);
                            thumbnailInitialized = true;
                        }
                    }
                    if (_Thumbnail == null)
                    {
                        if (_vmFile.IsFile)
                        {
                            _Thumbnail = GetDefaultFileBigIcon();
                        }
                        else
                        {
                            _Thumbnail = GetDefaultFolderBigIcon();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _vmFile.LogException(ex);
                }

                return _Thumbnail;
            }
        }









        private IEnumerable<string> _Verbs;
        public IEnumerable<string> Verbs { get => _Verbs; set => SetProperty(ref _Verbs, value); }

        public IEnumerable<ICommand> FileCommands
        {
            get
            {
                List<ICommand> commands = new List<ICommand>
                {
                    VMFile.OpenCommand,
                    VMFile.EditCommand,
                    VMFile.OpenAsCommand,
                    VMFile.ExploreCommand,
                    VMFile.PrintCommand,
                    SeparatorDummyCommand.Instance
                };
                TaskHelper.SafeExecute(() =>
                {
                    bool added = false;
                    foreach (ShellCommandSoftware item in DefaultEditors.Editors)
                    {
                        if (item.ForFolder || _vmFile.IsFile)
                        {
                            added = true;
                            string display = item.Id;
                            if (display.StartsWith("loc:", StringComparison.OrdinalIgnoreCase))
                            {
                                display = LocExtension.GetLocalizedValue<string>(display.Remove(0, 4));
                            }

                            if (string.IsNullOrEmpty(display))
                            {
                                display = item.Id;
                            }

                            DirectCommand command = new DirectCommand(item.Id, display, null, typeof(VMFile), _vmFile.ExecuteCommand, _vmFile.CanExecuteCommand)
                            {
                                Tag = item.Name
                            };

                            if (item.Icon != null)
                            {
                                command.Icon = new Image
                                {
                                    Source = item.Icon,
                                    Width = 16,
                                    Height = 16
                                };
                            }
                            commands.Add(command);
                        }
                    }
                    if (added)
                    {
                        commands.Add(SeparatorDummyCommand.Instance);
                    }

                    ShellCommandRoot root = ShellHelper.GetShellCommands(_vmFile.Path, !_vmFile.IsFile);
                    string[] verbs = root.Softwares.SelectMany(T => T.Verbs).Select(T => T.Verb).Distinct().ToArray();
                    Verbs = verbs;
                    if (verbs.Length > 0)
                    {
                        foreach (ShellCommandSoftware soft in root.Softwares)
                        {
                            ParentCommand parent = new ParentCommand(soft.Id, soft.Name, typeof(VMFile));

                            if (soft.Icon != null)
                            {
                                parent.Icon = new Image
                                {
                                    Source = soft.Icon,
                                    Width = 16,
                                    Height = 16
                                };
                            }

                            foreach (ShellCommandVerb verb in soft.Verbs)
                            {
                                //TODO ------------------------>
                                if (!verb.Verb.ToUpperInvariant().Contains("NEW"))// && !string.IsNullOrEmpty(verb.Command))
                                {
                                    DirectCommand cmd = new DirectCommand(verb.Verb, verb.Name.Replace("&", "_"), null, typeof(VMFile), _vmFile.ExecuteCommand, _vmFile.CanExecuteCommand)
                                    {
                                        Tag = verb.Command
                                    };
                                    if (verb.Verb.ToUpperInvariant().Contains("PRINT"))
                                    {
                                        cmd.Icon = VMFile.PrintCommand.Icon;
                                    }

                                    parent.Childs.Add(cmd);
                                }
                            }
                            if (parent.Childs.Count > 0)
                            {
                                commands.Add(parent);
                            }
                        }
                        commands.Add(SeparatorDummyCommand.Instance);
                    }

                    //commands.Add(VMFile.FindCommand);
                    //commands.Add(SeparatorDummyCommand.Instance);
                    commands.Add(VMFile.DeleteCommand);
                    commands.Add(VMFile.PermanentDeleteCommand);
                    commands.Add(SeparatorDummyCommand.Instance);
                    commands.Add(VMFile.PropertiesCommand);
                });
                return commands;
            }
        }


    }
}
