using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WPFByYourCommand.Commands;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.ViewModel
{
    [DebuggerDisplay("{GetType().Name}: {Name}")]
    public class VMFile : CommandViewModel
    {
        private const string MessageIsNotVMFile = "OriginalSource is not VMFile";



        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx OpenCommand = new RoutedCommandEx("open", "loc:PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx EditCommand = new RoutedCommandEx("edit", "loc:Edit", typeof(VMFile), new KeyGesture(Key.E, ModifierKeys.Control, "loc:EditKey"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx OpenAsCommand = new RoutedCommandEx("openas", "loc:OpenAs", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "loc:OpenAsKey"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx PrintCommand = new RoutedCommandEx("print", "loc:PresentationCore:ExceptionStringTable:PrintText", "pack://application:,,,/SizeOnDisk;component/Icons/PrintHS.png", typeof(VMFile), new KeyGesture(Key.P, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:PrintKeyDisplayString"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx ExploreCommand = new RoutedCommandEx("select", "loc:Explore", "pack://application:,,,/SizeOnDisk;component/Icons/Explore.png", typeof(VMFile), new KeyGesture(Key.N, ModifierKeys.Control, "ExploreKey"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx FindCommand = new RoutedCommandEx("find", "loc:PresentationCore:ExceptionStringTable:FindText", "pack://application:,,,/SizeOnDisk;component/Icons/SearchFolderHS.png", typeof(VMFile), new KeyGesture(Key.F, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:FindKeyDisplayString"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx DeleteCommand = new RoutedCommandEx("delete", "loc:PresentationCore:ExceptionStringTable:DeleteText", "pack://application:,,,/SizeOnDisk;component/Icons/Recycle_Bin.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:DeleteKeyDisplayString"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx PermanentDeleteCommand = new RoutedCommandEx("permanentdelete", "loc:PermanentDelete", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.Shift, "loc:PermanentDeleteKey"));
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx PropertiesCommand = new RoutedCommandEx("properties", "loc:PresentationCore:ExceptionStringTable:PropertiesText", "pack://application:,,,/SizeOnDisk;component/Icons/Properties.png", typeof(VMFile), new KeyGesture(Key.F4, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:PropertiesKeyDisplayString"));


        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                return;
            bindingCollection.Add(new CommandBinding(OpenCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(OpenAsCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(EditCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(PrintCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ExploreCommand, CallShellCommand, CanCallShellCommand));
            //bindingCollection.Add(new CommandBinding(FindCommand, CallShellCommand, CanCallShellCommand));
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
        internal VMFile(VMFolder parent, string name, string path, int? fileSize) : this(parent, name, path)
        {
            if (fileSize.HasValue)
            {
                FileSize = fileSize;
                DiskSize = Convert.ToInt64(Math.Floor((double)(fileSize / 4096)) * 4096);
            }
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

        public virtual string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(Name).Replace(".", "");
            }
        }

        public virtual bool IsFile
        {
            get
            {
                return true;
            }
        }

        protected static void ExecuteTask(Action<ParallelOptions> action, ParallelOptions parallelOptions = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            try
            {
                action(parallelOptions);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
        }

        protected virtual Task ExecuteTaskAsync(Action<ParallelOptions> action, bool highpriority = false)
        {
            return Parent?.ExecuteTaskAsync(action, highpriority);
        }

        public void Rename(string newName)
        {
            if (this.Name != newName)
            {
                ExecuteTask((parallelOptions) =>
                {
                    string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), newName);
                    if (this.IsFile)
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
                    this.OnPropertyChanged(nameof(Extension));
                    this.RefreshOnView();
                });
                this.Parent.RefreshAfterCommand();
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

        [SuppressMessage("Design","CA2213")]
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
            OnPropertyChanged(nameof(Details));
            this.Refresh(fileInfo);
        }

        #endregion functions

        #region Commands


        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private static void CallDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);

            if (file.IsSelected)
            {
                file.Parent.DeleteAllSelectedFiles();
            }
            else
            {
                ExecuteTask((parallelOptions) =>
                {
                    if (Shell.IOHelper.SafeNativeMethods.MoveToRecycleBin(file.Path))
                    {
                        file.Parent.RefreshAfterCommand();
                    }
                });
            }
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private static void CallPermanentDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);

            if (file.IsSelected)
            {
                file.Parent.PermanentDeleteAllSelectedFiles();
            }
            else
            {
                ExecuteTask((parallelOptions) =>
                {
                    if (Shell.IOHelper.SafeNativeMethods.PermanentDelete(file.Path))
                    {
                        file.Parent.RefreshAfterCommand();
                    }
                });
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

            if (command == PropertiesCommand || command == ExploreCommand || command == FindCommand)
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
            if (command == OpenCommand && !file.IsFile)
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
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private static void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentOutOfRangeException(nameof(e), MessageIsNotVMFile);

            if (!(e.Command is RoutedCommand command))
                throw new ArgumentOutOfRangeException(nameof(e), "Command is not RoutedCommand");

            if ((e.Command as IMenuCommand)?.Tag != null)
            {
                file.ExecuteCommand(command as IMenuCommand, e);
                return;
            }

            if (command == OpenAsCommand)
            {
                ShellHelper.ShellExecute("Rundll32.exe", $"Shell32.dll,OpenAs_RunDLL {file.Path}");
                return;
            }
            if (command == ExploreCommand)
            {
                ShellHelper.ShellExecute("explorer.exe", $"/select,\"{file.Path}\"");
                return;
            }
            string path = file.Path;
            if (e.Command == FindCommand && (file.IsFile || file.IsProtected))
                path = file.Parent.Path;
            ShellHelper.ShellExecute(path, null, command.Name.ToLowerInvariant());
        }

        #endregion Commands

        private bool CanExecuteCommand(IMenuCommand command, object parameter)
        {
            return !this.IsProtected && !string.IsNullOrEmpty(command.Tag);
        }

        private void ExecuteCommand(IMenuCommand command, object parameter)
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
                ShellHelper.ShellExecute(cmd, $"\"{this.Path}\"");
            }
            else if (cmd.StartsWith("dll:", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException($"Name:{command.Name}, Text:{command.Text}, Command{command.Tag}");
            }
            else
            {
                Tuple<string, string> cmdParam = ShellHelper.SplitCommandAndParameters(cmd);
                string parameters = cmdParam.Item2;

                string workingDirectory = this.Path;
                if (this.IsFile)
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

                ShellHelper.ShellExecute(cmdParam.Item1, parameters);
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
                ExecuteTask((parallelOptions) =>
                {
                    bool added = false;
                    foreach (ShellCommandSoftware item in DefaultEditors.Editors)
                    {
                        added = true;
                        string display = item.Id;
                        if (display.StartsWith("loc:", StringComparison.OrdinalIgnoreCase))
                            display = LocExtension.GetLocalizedValue<string>(display.Remove(0, 4));
                        if (string.IsNullOrEmpty(display))
                            display = item.Id;

                        DirectCommand command = new DirectCommand(item.Id, display, null, typeof(VMFile), ExecuteCommand, CanExecuteCommand)
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
                    if (added)
                        commands.Add(SeparatorDummyCommand.Instance);

                    ShellCommandRoot root = ShellHelper.GetShellCommands(this.Path, !this.IsFile);
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
                                    DirectCommand cmd = new DirectCommand(verb.Verb, verb.Name.Replace("&", "_"), null, typeof(VMFile), ExecuteCommand, CanExecuteCommand)
                                    {
                                        Tag = verb.Command
                                    };
                                    if (verb.Verb.ToUpperInvariant().Contains("PRINT"))
                                        cmd.Icon = PrintCommand.Icon;
                                    parent.Childs.Add(cmd);
                                }
                            }
                            //if (parent.Childs.Count == 1)
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


        protected override void InternalDispose(bool disposing)
        {
            if (disposing && this.Details != null)
                this.Details.Dispose();

            base.InternalDispose(disposing);
        }

    }
}
