using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.ViewModel
{
    public class VMFile : CommandViewModel
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

        private long? _DiskSize = null;
        private long? _FileSize = null;
        private long? _FileCount = 1;
        private long? _FolderCount = null;

        private bool _IsProtected = false;

        #endregion fields

        #region constructor

        [DesignOnly(true)]
        internal VMFile(VMFolder parent, string name) : this(parent, name, null)
        {

        }

        internal VMFile(VMFolder parent, string name, string path)
        {
            _Name = name;
            Path = path;
            Parent = parent;
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

        public long? FileCount
        {
            get { return _FileCount; }
            protected set { SetProperty(ref _FileCount, value); }
        }

        public long? FolderCount
        {
            get { return _FolderCount; }
            protected set { SetProperty(ref _FolderCount, value); }
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
            set { SetProperty(ref _isSelected, value); }
        }

        #endregion properties

        #region functions

        protected virtual void SelectItem()
        {
            this.Parent.SelectItem();
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

        //For VisualStudio Watch
        public override string ToString()
        {
            return string.Concat(this.GetType().Name, ": ", Path);
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

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                return;

            bool isFolder = file is VMFolder;
            if (command == PropertiesCommand)
            {
                e.CanExecute = true;
                return;
            }
            if (isFolder && file.IsProtected)
            {
                e.CanExecute = false;
                return;
            }
            if (command == ExploreCommand || command == FindCommand)
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

            if (command == OpenCommand && string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(file.Path)))
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = ShellHelper.GetVerbs(file.Path, file is VMFolder).Any(T => T.verb == command.Name);


            //e.CanExecute = ShellHelper.CanCallShellCommand(file.Path, command.Name);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        protected virtual void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                throw new ArgumentNullException("e", "Command is not RoutedCommand");

            if (file is VMFolder && command == OpenCommand)
                ShellHelper.ShellExecute(file.Path, OpenCommand.Name.ToLowerInvariant(), new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
            else
            {
                string path = file.Path;
                if (e.Command == ExploreCommand && !(file is VMFolder))
                    path = file.Parent.Path;
                ShellHelper.ShellExecute(path, command.Name.ToLowerInvariant(), new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
            }
        }

        #endregion Commands

        private void ExecuteCommand(DirectCommand command, object parameter)
        {
            string cmd = command.Name;

            if (cmd.Contains("%1"))
                cmd = cmd.Replace("%1", this.Path);

            ShellHelper.ShellExecute("cmd", null, cmd, new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
        }

        public IList<CommandBinding> CommandsBindings { get; } = new List<CommandBinding>();

        public IEnumerable<ICommand> FileCommands
        {
            get
            {
                List<ICommand> commands = new List<ICommand>();
                commands.Add(VMFile.OpenCommand);
                commands.Add(VMFile.EditCommand);
                commands.Add(VMFile.OpenAsCommand);
                commands.Add(VMFile.PrintCommand);
                commands.Add(SeparatorDummyCommand.Instance);


                ShellHelper.ShellCommandRoot root = ShellHelper.GetShellCommands(this.Path, this is VMFolder);
                foreach (ShellHelper.ShellCommandSoftware soft in root.Softwares)
                {
                    ParentCommand parent = new ParentCommand(soft.Id, soft.Name, typeof(VMFile));

                    if (soft.Icon != null)
                    {
                        Image image = new Image();
                        image.Source = soft.Icon;
                        image.Width = 16;
                        image.Height = 16;
                        parent.Icon = image;
                    }                    

                    foreach (ShellHelper.ShellCommandVerb verb in soft.Verbs)
                    {
                        DirectCommand cmd = new DirectCommand(verb.Command, verb.Verb, null, typeof(VMFile), ExecuteCommand);

                        parent.Childs.Add(cmd);
                    }
                    //if (parent.Childs.Count == 1)
                    if (parent.Childs.Count > 0)
                        commands.Add(parent);
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
    }
}
