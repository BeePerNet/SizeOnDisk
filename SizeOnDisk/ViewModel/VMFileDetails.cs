using SizeOnDisk.Shell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinProps;
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

        private static BitmapImage DefaultFileBigIcon { get; } = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileBig.png");
        private static BitmapImage DefaultFileIcon { get; } = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileSmall.png");
        private static BitmapImage DefaultFolderBigIcon { get; } = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderBig.png");
        private static BitmapImage DefaultFolderIcon { get; } = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderSmall.png");


        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        [DesignOnly(true)]
        public VMFileDetails(VMFile vmFile, DateTime startdate)
        {
            _vmFile = vmFile;
            CreationTime = startdate.AddDays(-27.32);
            LastAccessTime = startdate.AddDays(-23.931111);
            LastWriteTime = startdate;
        }

        public LittleFileInfo Load()
        {
            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent?.Path, _vmFile.Name);
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
                            icon = DefaultFileIcon;
                        }
                        else
                        {
                            icon = DefaultFolderIcon;
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
                                            thumbnail = DefaultFileBigIcon;
                                        }
                                        else
                                        {
                                            thumbnail = DefaultFolderBigIcon;
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
                            _Thumbnail = DefaultFileBigIcon;
                        }
                        else
                        {
                            _Thumbnail = DefaultFolderBigIcon;
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
                    VMFile.ShellSelectCommand,
                    VMFile.PrintCommand,
                    SeparatorDummyCommand.Instance
                };
                if (_vmFile.IsLink)
                {
                    RoutedCommandEx linkCommand = VMFile.FollowLinkCommand;
                    linkCommand.Text = _vmFile.LinkPath;
                    commands.Add(linkCommand);
                }

                _vmFile.Root.ExecuteTask(() =>
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
                                    Stretch = Stretch.None
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
                }, false);
                return commands;
            }
        }




        IEnumerable<VMFileProperty> _Properties;
        public IEnumerable<VMFileProperty> Properties
        {
            get
            {
                if (_Properties == null && !_vmFile.Root.IsDesign)
                {
                    FillProperties();
                }
                return _Properties;
            }
        }


        private void FillProperties()
        {
            List<VMFileProperty> result = new List<VMFileProperty>();
            if (_vmFile.IsFile)
            {
                PropertyStore store = new PropertyStore(_vmFile.Path, PropertyStore.GetFlags.BestEffort);
                foreach (PropertyKey key in store)
                {
                    string name;
                    try
                    {
                        name = key.CanonicalName;
                        if (name.StartsWith("System."))
                            name = name.Remove(0, 7);

                        PropVariant variant = store.GetValue(key);

                        if (variant != null)
                        {
                            if (variant.Value != null)
                            {
                                if (variant.Value is string str)
                                {
                                    result.Add(new VMFileProperty(name, str));
                                }
                                else if (variant.Value is IEnumerable list)
                                {
                                    result.Add(new VMFileProperty(name, string.Join(Environment.NewLine, list.Cast<object>().Select(T => T.ToString()))));
                                }
                                else
                                {
                                    result.Add(new VMFileProperty(name, variant.Value.ToString()));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            else
            {
                //TODO: Add others or find if contains PropertyStore
                result.Add(new VMFileProperty(Languages.Localization.Name, _vmFile.Name));
                if (_vmFile.IsLink)
                {
                    result.Add(new VMFileProperty("Link.TargetParsingPath", _vmFile.LinkPath));
                }
            }
            _Properties = result.OrderBy(T => T.Name);
        }








    }
}
