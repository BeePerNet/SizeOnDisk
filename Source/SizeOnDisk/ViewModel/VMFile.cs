using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.ViewModel
{
    [DebuggerDisplay("{GetType().Name}: {Name}")]
    public class VMFile : CommandViewModel, IDisposable
    {
        public static readonly RoutedCommandEx OpenCommand = new RoutedCommandEx("open", "loc:PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        public static readonly RoutedCommandEx EditCommand = new RoutedCommandEx("edit", "loc:Edit", typeof(VMFile), new KeyGesture(Key.E, ModifierKeys.Control, "loc:EditKey"));
        public static readonly RoutedCommandEx OpenAsCommand = new RoutedCommandEx("openas", "loc:OpenAs", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "loc:OpenAsKey"));
        public static readonly RoutedCommandEx PrintCommand = new RoutedCommandEx("print", "loc:PresentationCore:ExceptionStringTable:PrintText", "pack://application:,,,/SizeOnDisk;component/Icons/PrintHS.png", typeof(VMFile), new KeyGesture(Key.P, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:PrintKeyDisplayString"));
        public static readonly RoutedCommandEx ExploreCommand = new RoutedCommandEx("explore", "loc:Explore", "pack://application:,,,/SizeOnDisk;component/Icons/Folder.png", typeof(VMFile), new KeyGesture(Key.N, ModifierKeys.Control, "ExploreKey"));
        public static readonly RoutedCommandEx FindCommand = new RoutedCommandEx("find", "loc:PresentationCore:ExceptionStringTable:FindText", "pack://application:,,,/SizeOnDisk;component/Icons/SearchFolderHS.png", typeof(VMFile), new KeyGesture(Key.F, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:FindKeyDisplayString"));
        public static readonly RoutedCommandEx DeleteCommand = new RoutedCommandEx("delete", "loc:PresentationCore:ExceptionStringTable:DeleteText", "pack://application:,,,/SizeOnDisk;component/Icons/Recycle_Bin_Empty.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:DeleteKeyDisplayString"));
        public static readonly RoutedCommandEx PermanentDeleteCommand = new RoutedCommandEx("permanentdelete", "loc:PermanentDelete", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.Shift, "loc:PermanentDeleteKey"));
        public static readonly RoutedCommandEx PropertiesCommand = new RoutedCommandEx("properties", "loc:PresentationCore:ExceptionStringTable:PropertiesText", typeof(VMFile), new KeyGesture(Key.F4, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:PropertiesKeyDisplayString"));


        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            bindingCollection.Add(new CommandBinding(OpenCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(OpenAsCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(EditCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(PrintCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ExploreCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(FindCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(DeleteCommand, CallDeleteCommand, CanCallDeleteCommand));
            bindingCollection.Add(new CommandBinding(PermanentDeleteCommand, CallPermanentDeleteCommand, CanCallDeleteCommand));
            bindingCollection.Add(new CommandBinding(PropertiesCommand, CallShellCommand, CanCallShellCommand));
        }

        public override void AddInputModels(InputBindingCollection bindingCollection)
        {
        }


        #region fields

        private string _Name;
        private bool _IsProtected = false;

        #endregion fields

        #region constructor

        internal VMFile(VMFolder parent, string name, string path)
        {
            _Name = name;
            Path = path;
            Parent = parent;
        }

        [DesignOnly(true)]
        internal VMFile(VMFolder parent, string name, string path, bool dummy) : this(parent, name, path)
        {
            DiskSize = 4096;
            FileSize = 12;
        }


        #endregion constructor

        #region properties

        public VMFolder Parent { get; }

        public string Path { get; private set; }

        public string Name
        {
            get { return _Name; }
            set
            {
                this.Rename(value);
            }
        }

        public void Rename(string newName)
        {
            if (this.Name != newName)
            {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), newName);
                if (this is VMFolder)
                {
                    File.Move(this.Path, newPath);
                }
                else
                {
                    Directory.Move(this.Path, newPath);
                }
                _Name = newName;
                Path = newPath;
                this.OnPropertyChanged(nameof(Name));
                this.OnPropertyChanged(nameof(Path));
            }
        }

        private long? _FileSize = null;
        private long? _DiskSize = null;

        public virtual long? FileTotal
        {
            get { return 1; }
            protected set { }
        }

        public virtual long? FolderTotal
        {
            get { return null; }
            protected set { }
        }

        public long? DiskSize
        {
            get { return _DiskSize; }
            protected set { SetProperty(ref _DiskSize, value); }
        }


        public long? FileSize
        {
            get { return _FileSize; }
            protected set { SetProperty(ref _FileSize, value); }
        }

        public bool IsProtected
        {
            get { return _IsProtected; }
            protected set { SetProperty(ref _IsProtected, value); }
        }


        private bool _isSelected = false;
        public virtual bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                SetProperty(ref _isSelected, value);
                if (value)
                    SelectListItem(this);
            }
        }

        #endregion properties

        #region functions

        protected virtual void SelectItem()
        {
            this.Parent.SelectItem();
        }

        protected virtual void SelectListItem(VMFile selected)
        {
            this.Parent.SelectListItem(selected);
        }

        internal virtual void Refresh(LittleFileInfo fileInfo)
        {
            this.Attributes = fileInfo.Attributes;
            this.FileSize = fileInfo.Size;
            /*this.DiskSize = ((((fileInfo.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed ?
                fileInfo.CompressedSize : this.FileSize)
                + this.Parent.ClusterSize - 1) / this.Parent.ClusterSize) * this.Parent.ClusterSize;*/
            //            if ((this.Attributes & FileAttributes.Normal) != FileAttributes.Normal)
            //              this.DiskSize = 0;
            this.DiskSize = (((fileInfo.CompressedSize ?? this.FileSize) + this.Parent.ClusterSize - 1) / this.Parent.ClusterSize) * this.Parent.ClusterSize;
        }

        private FileAttributes _Attributes = FileAttributes.Normal;
        public FileAttributes Attributes
        {
            get { return _Attributes; }
            protected set
            {
                SetProperty(ref _Attributes, value);
            }
        }

        VMFileDetails _Details;
        public VMFileDetails Details
        {
            get
            {
                return _Details;
            }
        }

        public void RefreshOnView()
        {
            _Details = new VMFileDetails(this);
            LittleFileInfo fileInfo = _Details.Load();
            this.Refresh(fileInfo);
            OnPropertyChanged(nameof(Details));
        }

        #endregion functions

        #region Commands


        private static void CallDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            if (file.IsSelected)
            {
                file.Parent.DeleteAllSelectedFiles();
            }
            else
            {
                if (Shell.IOHelper.SafeNativeMethods.MoveToRecycleBin(file.Path))
                {
                    file.Parent.FillChildList();
                    file.Parent.RefreshCount();
                    file.Parent.RefreshParents();
                }
            }
        }

        private static void CallPermanentDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            if (file.IsSelected)
            {
                file.Parent.PermanentDeleteAllSelectedFiles();
            }
            else
            {
                if (Shell.IOHelper.SafeNativeMethods.PermanentDelete(file.Path))
                {
                    file.Parent.FillChildList();
                    file.Parent.RefreshCount();
                    file.Parent.RefreshParents();
                }
            }
        }

        private static void CanCallDeleteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                return;

            e.CanExecute = !file.IsProtected && !(file is VMRootFolder);
        }

        private static void CanCallShellCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                return;

            if (!(e.Command is RoutedCommand command))
                return;

            bool isFolder = file is VMFolder;
            if (command == PropertiesCommand)
            {
                e.CanExecute = true;
                return;
            }
            if (command == ExploreCommand)
            {
                e.CanExecute = true;
                return;
            }
            if (file.IsProtected)
            {
                e.CanExecute = false;
                return;
            }
            if (command == OpenAsCommand)
            {
                e.CanExecute = true;
                return;
            }
            if (command == FindCommand && !isFolder)
            {
                e.CanExecute = true;
                return;
            }
            if (command == OpenCommand && isFolder)
            {
                e.CanExecute = true;
                return;
            }
            if (command == OpenCommand && string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(file.Path)))
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = file.Verbs != null && file.Verbs.Any(T => T == command.Name);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        private static void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            if (!(e.Command is RoutedCommand command))
                throw new ArgumentNullException("e", "Command is not RoutedCommand");

            if (command == OpenAsCommand)
            {
                ShellHelper.ShellExecuteOpenAs(file.Path);
                return;
            }
            bool isFolder = file is VMFolder;
            if (isFolder && command == OpenCommand)
            {
                ShellHelper.ShellExecute(file.Path, null, OpenCommand.Name.ToLowerInvariant());
                return;
            }
            string path = file.Path;
            if ((e.Command == ExploreCommand || e.Command == FindCommand) && (!isFolder || file.IsProtected))
                path = file.Parent.Path;
            ShellHelper.ShellExecute(path, null, command.Name.ToLowerInvariant());
        }

        #endregion Commands

        private bool CanExecuteCommand(DirectCommand command, object parameter)
        {
            return !this.IsProtected && !string.IsNullOrEmpty(command.Tag);
        }

        private void ExecuteCommand(DirectCommand command, object parameter)
        {
            string cmd = command.Tag;
            if (cmd.StartsWith("Id:", StringComparison.Ordinal))
            {
                cmd = cmd.Substring(3);
                ShellHelper.Activate(cmd, this.Path, command.Name);
            }
            else if (cmd.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
            {
                cmd = cmd.Substring(4);
                ShellHelper.ShellExecute(this.Path, null, cmd);
            }
            else if (cmd.StartsWith("dll:", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException($"Name:{command.Name}, Text:{command.Text}, Command{command.Tag}");
            }
            else
            {
                string parameters = string.Empty;
                int pos = 1;
                if (cmd.StartsWith("\"", StringComparison.Ordinal) && cmd.Count(T => T == '\"') > 1)
                {
                    while (pos < cmd.Length)
                    {
                        pos = cmd.IndexOf('\"', pos);
                        if (pos < 0 || pos >= cmd.Length)
                            pos = cmd.Length;
                        else if (cmd[pos + 1] != '\"')
                        {
                            parameters = cmd.Substring(pos + 1);
                            cmd = cmd.Substring(0, pos + 1);
                            pos = cmd.Length;
                        }
                    }
                }
                else
                {
                    pos = cmd.IndexOf(' ');
                    if (pos > 0 && pos < cmd.Length)
                    {
                        parameters = cmd.Substring(pos + 1);
                        cmd = cmd.Substring(0, pos + 1);
                    }
                }
                string workingDirectory = this.Path;
                if (!(this is VMFolder))
                    workingDirectory = this.Parent.Path;

                if (parameters.Contains('%'))
                {
                    parameters = Regex.Replace(parameters, "%1", this.Path, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%l", this.Path, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%v", workingDirectory, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%w", workingDirectory, RegexOptions.IgnoreCase);
                }
                else
                {
                    parameters = string.Concat(parameters, "\"", this.Path, "\"");
                }

                ShellHelper.ShellExecute(cmd, parameters);
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
                    VMFile.PrintCommand
                };

                ShellCommandRoot root = ShellHelper.GetShellCommands(this.Path, this is VMFolder);
                string[] verbs = root.Softwares.SelectMany(T => T.Verbs).Select(T => T.Verb).Distinct().ToArray();
                this.Verbs = verbs;
                if (verbs.Length > 0)
                {
                    commands.Add(SeparatorDummyCommand.Instance);
                    foreach (ShellCommandSoftware soft in root.Softwares)
                    {
                        ParentCommand parent = new ParentCommand(soft.Id, soft.Name, typeof(VMFile));

                        if (soft.Icon != null)
                        {
                            Image image = new Image
                            {
                                Source = soft.Icon,
                                Width = 16,
                                Height = 16
                            };
                            parent.Icon = image;
                        }

                        foreach (ShellCommandVerb verb in soft.Verbs)
                        {
                            //TODO ------------------------>
                            if (!verb.Verb.ToUpperInvariant().Contains("NEW"))// && !string.IsNullOrEmpty(verb.Command))
                            {
                                DirectCommand cmd = new DirectCommand(verb.Verb, verb.Name.Replace("&", ""), null, typeof(VMFile), ExecuteCommand, CanExecuteCommand)
                                {
                                    Tag = verb.Command
                                };
                                parent.Childs.Add(cmd);
                            }
                        }
                        //if (parent.Childs.Count == 1)
                        if (parent.Childs.Count > 0)
                        {
                            commands.Add(parent);
                        }
                    }
                }

                commands.Add(SeparatorDummyCommand.Instance);
                commands.Add(VMFile.ExploreCommand);
                commands.Add(VMFile.FindCommand);
                commands.Add(SeparatorDummyCommand.Instance);
                commands.Add(VMFile.DeleteCommand);
                commands.Add(VMFile.PermanentDeleteCommand);
                commands.Add(SeparatorDummyCommand.Instance);
                commands.Add(VMFile.PropertiesCommand);
                return commands;
            }
        }



        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (Details != null)
                    this.Details.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
