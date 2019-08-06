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
using WPFByYourCommand.Exceptions;
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
                defaultFileBigIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileBig.png");
            return defaultFileBigIcon;
        }

        private static BitmapImage defaultFileIcon;
        private static BitmapImage GetDefaultFileIcon()
        {
            if (defaultFileIcon == null)
                defaultFileIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileSmall.png");
            return defaultFileIcon;
        }

        private static BitmapImage defaultFolderBigIcon;
        private static BitmapImage GetDefaultFolderBigIcon()
        {
            if (defaultFolderBigIcon == null)
                defaultFolderBigIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderBig.png");
            return defaultFolderBigIcon;
        }

        private static BitmapImage defaultFolderIcon;
        private static BitmapImage GetDefaultFolderIcon()
        {
            if (defaultFolderIcon == null)
                defaultFolderIcon = ImagingHelper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderSmall.png");
            return defaultFolderIcon;
        }

        public VMFileDetails(bool isFile)
        {
            if (isFile)
                this._Thumbnail = GetDefaultFileBigIcon();
            else
                this._Thumbnail = GetDefaultFolderBigIcon();
            thumbnailInitialized = true;
        }

        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        public LittleFileInfo Load()
        {
            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent.Path, _vmFile.Name);
            this.CreationTime = fileInfo.CreationTime;
            this.LastAccessTime = fileInfo.LastAccessTime;
            this.LastWriteTime = fileInfo.LastWriteTime;

            this.thumbnailInitialized = false;
            this.OnPropertyChanged(nameof(Thumbnail));

            return fileInfo;
        }

        public string FileType
        {
            get
            {
                if (_vmFile != null && _vmFile.IsFile)
                    return ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
                else
                    return string.Empty;
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
                    if (_vmFile == null)
                    {
                        if (Thumbnail == GetDefaultFileBigIcon())
                            icon = GetDefaultFileIcon();
                        else
                            icon = GetDefaultFolderIcon();
                    }
                    else
                    {
                        icon = ShellHelper.GetIcon(_vmFile.Path, 16);
                        if (icon == null)
                        {
                            if (_vmFile.IsFile)
                                icon = GetDefaultFileIcon();
                            else
                                icon = GetDefaultFolderIcon();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.ShowException(ex);
                    this._vmFile?.Root.LogException(ex);
                }
                return icon;
            }
        }


        private bool thumbnailInitialized = false;
        //Seems to have problems with VOB
        BitmapSource _Thumbnail = null;
        public BitmapSource Thumbnail
        {
            get
            {
                try
                {
                    if (!thumbnailInitialized)
                    {
                        if (_Thumbnail == null)
                            this._Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);
                        if (this._Thumbnail == null)
                        {
                            if (_vmFile.IsFile)
                                this._Thumbnail = GetDefaultFileBigIcon();
                            else
                                this._Thumbnail = GetDefaultFolderBigIcon();
                        }
                        else
                        {
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    _Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                                    OnPropertyChanged(nameof(Thumbnail));
                                }
                                catch (Exception ex)
                                {
                                    ExceptionBox.ShowException(ex);
                                    this._vmFile?.Root.LogException(ex);
                                }
                            }, TaskCreationOptions.LongRunning);
                        }
                        thumbnailInitialized = true;
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.ShowException(ex);
                    this._vmFile?.Root.LogException(ex);
                }

                return _Thumbnail;
            }
        }









        private IEnumerable<string> _Verbs;
        public IEnumerable<string> Verbs { get { return _Verbs; } set { SetProperty(ref _Verbs, value); } }

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
                        if (item.ForFolder || this._vmFile.IsFile)
                        {
                            added = true;
                            string display = item.Id;
                            if (display.StartsWith("loc:", StringComparison.OrdinalIgnoreCase))
                                display = LocExtension.GetLocalizedValue<string>(display.Remove(0, 4));
                            if (string.IsNullOrEmpty(display))
                                display = item.Id;

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
                        commands.Add(SeparatorDummyCommand.Instance);

                    ShellCommandRoot root = ShellHelper.GetShellCommands(this._vmFile.Path, !this._vmFile.IsFile);
                    string[] verbs = root.Softwares.SelectMany(T => T.Verbs).Select(T => T.Verb).Distinct().ToArray();
                    this.Verbs = verbs;
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
                                    DirectCommand cmd = new DirectCommand(verb.Verb, verb.Name.Replace("&", "_"), null, typeof(VMFile), this._vmFile.ExecuteCommand, this._vmFile.CanExecuteCommand)
                                    {
                                        Tag = verb.Command
                                    };
                                    if (verb.Verb.ToUpperInvariant().Contains("PRINT"))
                                        cmd.Icon = VMFile.PrintCommand.Icon;
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
