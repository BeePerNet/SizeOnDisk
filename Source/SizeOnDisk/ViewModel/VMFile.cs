using SizeOnDisk.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.ViewModel
{
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
    [DebuggerDisplay("{GetType().Name}: {Name}")]
    public class VMFile : CommandViewModel
    {
        private const string MessageIsNotVMFile = "OriginalSource is not VMFile";
               

        public static readonly RoutedCommandEx OpenCommand = new RoutedCommandEx("open", "loc:PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        public static readonly RoutedCommandEx EditCommand = new RoutedCommandEx("edit", "loc:Edit", typeof(VMFile), new KeyGesture(Key.E, ModifierKeys.Control, "loc:EditKey"));
        public static readonly RoutedCommandEx OpenAsCommand = new RoutedCommandEx("openas", "loc:OpenAs", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "loc:OpenAsKey"));
        public static readonly RoutedCommandEx PrintCommand = new RoutedCommandEx("print", "loc:PresentationCore:ExceptionStringTable:PrintText", "pack://application:,,,/SizeOnDisk;component/Icons/PrintHS.png", typeof(VMFile), new KeyGesture(Key.P, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:PrintKeyDisplayString"));
        public static readonly RoutedCommandEx ExploreCommand = new RoutedCommandEx("select", "loc:Explore", "pack://application:,,,/SizeOnDisk;component/Icons/Explore.png", typeof(VMFile), new KeyGesture(Key.N, ModifierKeys.Control, "ExploreKey"));
        public static readonly RoutedCommandEx FindCommand = new RoutedCommandEx("find", "loc:PresentationCore:ExceptionStringTable:FindText", "pack://application:,,,/SizeOnDisk;component/Icons/SearchFolderHS.png", typeof(VMFile), new KeyGesture(Key.F, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:FindKeyDisplayString"));
        public static readonly RoutedCommandEx DeleteCommand = new RoutedCommandEx("delete", "loc:PresentationCore:ExceptionStringTable:DeleteText", "pack://application:,,,/SizeOnDisk;component/Icons/Recycle_Bin.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:DeleteKeyDisplayString"));
        public static readonly RoutedCommandEx PermanentDeleteCommand = new RoutedCommandEx("permanentdelete", "loc:PermanentDelete", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.Shift, "loc:PermanentDeleteKey"));
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



        private string _Name;


        #region constructor


        internal VMFile(VMFolder parent, string name)
        {
            _Name = name;
            Parent = parent;
        }


        [DesignOnly(true)]
        internal VMFile(VMFolder parent, string name, ulong? fileSize) : this(parent, name)
        {
            if (fileSize.HasValue)
            {
                FileSize = fileSize;
                DiskSize = Convert.ToUInt64(Math.Ceiling((double)fileSize / 4096) * 4096);
                _Details = new VMFileDetails(this);
            }
        }


        #endregion constructor

        #region properties

        public VMFolder Parent { get; }

        public virtual string Path
        {
            get
            {
                if (Parent.Path == null)
                {

                }
                return System.IO.Path.Combine(Parent.Path, Name);
            }
        }

        public string Name
        {
            get => _Name;
            set => Rename(value);
        }

        public virtual string Extension => System.IO.Path.GetExtension(Name).Replace(".", "");

        public virtual bool IsFile => true;


        public virtual Task ExecuteTaskAsync(Action action, bool highpriority = false)
        {
            return Parent.ExecuteTaskAsync(action, highpriority);
        }

        public void Rename(string newName)
        {
            if (Name != newName)
            {
                ExecuteTaskAsync(() =>
                {
                    string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), newName);
                    if (IsFile)
                    {
                        File.Move(Path, newPath);
                    }
                    else
                    {
                        Directory.Move(Path, newPath);
                    }
                    _Name = newName;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Path));
                    OnPropertyChanged(nameof(Extension));
                    RefreshOnView();
                    Parent.RefreshAfterCommand();
                });
            }
        }

        private ulong? _FileSize = null;
        private ulong? _DiskSize = null;

        public virtual ulong? FileTotal
        {
            get { return 1; }
            protected set { }
        }

        public virtual ulong? FolderTotal
        {
            get { return null; }
            protected set { }
        }

        public ulong? DiskSize
        {
            get { return _DiskSize; }
            protected set { SetProperty(ref _DiskSize, value); }
        }


        public ulong? FileSize
        {
            get { return _FileSize; }
            protected set { SetProperty(ref _FileSize, value); }
        }

        public bool IsProtected
        {
            get { return (_Attributes & FileAttributesEx.Protected) == FileAttributesEx.Protected; }
            protected set
            {
                if (value != IsProtected)
                {
                    if (value)
                        _Attributes |= FileAttributesEx.Protected;
                    else
                        _Attributes &= ~FileAttributesEx.Protected;
                    OnPropertyChanged(nameof(IsProtected));
                }
            }
        }


        public bool IsSelected
        {
            get { return (_Attributes & FileAttributesEx.Selected) == FileAttributesEx.Selected; }
            set
            {
                if (value != IsSelected)
                {
                    if (value)
                        _Attributes |= FileAttributesEx.Selected;
                    else
                        _Attributes &= ~FileAttributesEx.Selected;
                    OnPropertyChanged(nameof(IsSelected));
                    if (value)
                        SelectListItem(this);
                }
            }
        }

        #endregion properties

        #region functions


        protected virtual void SelectListItem(VMFile selected)
        {
            Parent.SelectListItem(selected);
        }

        public bool IsLink => ((Attributes & FileAttributesEx.ReparsePoint) == FileAttributesEx.ReparsePoint)
                    || (IsFile && Extension.ToUpperInvariant() == "LNK");


        internal virtual void Refresh(LittleFileInfo fileInfo)
        {
            _Attributes = ((FileAttributesEx)fileInfo.Attributes) | (_Attributes & FileAttributesEx.ExMask);
            OnPropertyChanged(nameof(IsLink));
            if (IsFile)
            {
                FileSize = fileInfo.Size;
                DiskSize = (((fileInfo.CompressedSize ?? FileSize) + Root.ClusterSize - 1) / Root.ClusterSize) * Root.ClusterSize;
            }

        }

        protected FileAttributesEx _Attributes = FileAttributesEx.Normal;
        public FileAttributesEx Attributes
        {
            get { return _Attributes; }
        }

        [SuppressMessage("Design", "CA2213")]
        VMFileDetails _Details;
        public VMFileDetails Details
        {
            get
            {
                if (_Details == null)
                    _Details = new VMFileDetails(this);
                return _Details;
            }
            private set
            {
                SetProperty(ref _Details, value);
            }
        }

        public void RefreshOnView()
        {
            if (Details == null)
                Details = new VMFileDetails(this);
            LittleFileInfo fileInfo = Details.Load();
            Refresh(fileInfo);
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
                file.ExecuteTaskAsync(() =>
                {
                    if (IOHelper.SafeNativeMethods.MoveToRecycleBin(file.Path))
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
                TaskHelper.SafeExecute(() =>
                {
                    if (IOHelper.SafeNativeMethods.PermanentDelete(file.Path))
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

            e.CanExecute = file.Details != null && file.Details.Verbs != null && file.Details.Verbs.Any(T => T == command.Name);
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

        internal bool CanExecuteCommand(IMenuCommand command, object _)
        {
            return !IsProtected && !string.IsNullOrEmpty(command.Tag);
        }

        internal void ExecuteCommand(IMenuCommand command, object _)
        {
            string cmd = command.Tag;
            if (cmd.StartsWith("Id:", StringComparison.Ordinal))
            {
                cmd = cmd.Substring(3);
                ShellHelper.Activate(cmd, Path, command.Name);
            }
            else if (cmd.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
            {
                cmd = cmd.Substring(4);
                ShellHelper.ShellExecute(cmd, $"\"{Path}\"");
            }
            else if (cmd.StartsWith("dll:", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException($"Name:{command.Name}, Text:{command.Text}, Command{command.Tag}");
            }
            else
            {
                Tuple<string, string> cmdParam = ShellHelper.SplitCommandAndParameters(cmd);
                string parameters = cmdParam.Item2;

                string workingDirectory = Path;
                if (IsFile)
                    workingDirectory = Parent.Path;

                if (parameters.Contains('%'))
                {
                    parameters = Regex.Replace(parameters, "%1", Path, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%l", Path, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%v", workingDirectory, RegexOptions.IgnoreCase);
                    parameters = Regex.Replace(parameters, "%w", workingDirectory, RegexOptions.IgnoreCase);
                }
                else
                {
                    parameters = string.Concat(parameters, "\"", Path, "\"");
                }

                ShellHelper.ShellExecute(cmdParam.Item1, parameters);
            }
        }


        public virtual VMRootFolder Root
        {
            get
            {
                return Parent.Root;
            }
        }

        public void LogException(Exception ex)
        {
            TextExceptionFormatter formatter = new TextExceptionFormatter(ex);
            Root.Log(new VMLog(this, formatter.GetInnerException().Message, formatter.Format()));
        }



    }
}
