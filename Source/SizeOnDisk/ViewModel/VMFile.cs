using SizeOnDisk.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFByYourCommand;

namespace SizeOnDisk.ViewModel
{
    public class VMFile : CommandViewModel
    {
        #region fields

        private VMFolder _Parent;
        private string _Path;
        private string _Name;

        private long? _DiskSize = null;
        private long? _FileSize = null;
        private long? _FileCount = 1;
        private long? _FolderCount = null;

        private bool _IsProtected = false;

        private bool _isSelected;

        #endregion fields

        #region constructor

        internal VMFile(VMFolder parent, string name, string path)
        {
            _Name = name;
            _Path = path;
            _Parent = parent;
        }

        #endregion constructor

        #region properties

        public VMFolder Parent
        {
            get { return _Parent; }
        }

        public string Path
        {
            get { return _Path; }
        }

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
                if (this.IsFile)
                {
                    File.Move(this.Path, newPath);
                }
                else
                {
                    Directory.Move(this.Path, newPath);
                }
                _Name = newName;
                _Path = newPath;
                this.OnPropertyChanged("Name");
                this.OnPropertyChanged("Path");
            }
        }

        public virtual bool IsFile
        {
            get { return true; }
        }

        public long? FileCount
        {
            get { return _FileCount; }
            protected set
            {
                if (value != _FileCount)
                {
                    _FileCount = value;
                    this.OnPropertyChanged("FileCount");
                }
            }
        }

        public long? FolderCount
        {
            get { return _FolderCount; }
            protected set
            {
                if (value != _FolderCount)
                {
                    _FolderCount = value;
                    this.OnPropertyChanged("FolderCount");
                }
            }
        }

        public long? DiskSize
        {
            get { return _DiskSize; }
            protected set
            {
                if (value != _DiskSize)
                {
                    _DiskSize = value;
                    this.OnPropertyChanged("DiskSize");
                }
            }
        }

        public long? FileSize
        {
            get { return _FileSize; }
            protected set
            {
                if (value != _FileSize)
                {
                    _FileSize = value;
                    this.OnPropertyChanged("FileSize");
                }
            }
        }

        public bool IsProtected
        {
            get { return _IsProtected; }
            protected set
            {
                if (value != _IsProtected)
                {
                    _IsProtected = value;
                    this.OnPropertyChanged("IsProtected");
                }
            }
        }

        public virtual bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    if (_isSelected && this.Parent != null)
                    {
                        this._Parent.IsExpanded = true;
                        this.SelectItem();
                    }
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion properties

        #region functions

        protected virtual void SelectItem()
        {
            this._Parent.SelectItem();
        }

        public virtual void Refresh(uint clusterSize, ParallelOptions parallelOptions)
        {
            if (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested)
                return;

            LittleFileInfo fileInfo = new Utilities.LittleFileInfo(this.Path);
            this.FileSize = fileInfo.Size;
            this.DiskSize = ((((fileInfo.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed ?
                fileInfo.CompressedSize : this.FileSize)
                + clusterSize - 1) / clusterSize) * clusterSize;
        }

        private VMFileAttributes _Attributes = null;

        public VMFileAttributes Attributes
        {
            get { return _Attributes; }
        }

        public void RefreshOnView()
        {
            if (Attributes == null)
            {
                try
                {
                    _Attributes = new VMFileAttributes(this);
                }
                catch (UnauthorizedAccessException)
                {
                    this.IsProtected = true;
                }
                this.OnPropertyChanged("Attributes");
            }
        }

        //For VisualStudio Watch
        public override string ToString()
        {
            return string.Concat(this.GetType().Name, ": ", _Path);
        }

        #endregion functions

        #region Commands

        public static readonly CommandEx OpenCommand = new CommandEx("Open", "PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        public static readonly RoutedCommand OpenAsCommand = new RoutedCommand("OpenAs", typeof(VMFile));
        public static readonly RoutedCommand EditCommand = new RoutedCommand("Edit", typeof(VMFile));
        public static readonly RoutedCommand ExploreCommand = new RoutedCommand("explore", typeof(VMFile));
        public static readonly RoutedCommand PermanentDeleteCommand = new RoutedCommand("permanentdelete", typeof(VMFile));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            bindingCollection.Add(new CommandBinding(OpenCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(OpenAsCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ExploreCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(EditCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Find, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Print, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Properties, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Delete, CallDeleteCommand, CanCallDeleteCommand));
            bindingCollection.Add(new CommandBinding(PermanentDeleteCommand, CallPermanentDeleteCommand, CanCallDeleteCommand));
        }

        public override void AddInputModels(InputBindingCollection bindingCollection)
        {
            /*bindingCollection.Add(new InputBinding(EditCommand, new KeyGesture(Key.E, ModifierKeys.Control)));
            bindingCollection.Add(new InputBinding(PermanentDeleteCommand, new KeyGesture(Key.Delete, ModifierKeys.Shift)));*/
        }

        private static void CallDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            file.Delete();
        }

        private static void CallPermanentDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            file.PermanentDelete();
        }

        public void PermanentDelete()
        {
            if (IOHelper.SafeNativeMethods.PermanentDelete(new string[] { this.Path }))
            {
                if (!File.Exists(this.Path) && !Directory.Exists(this.Path))
                {
                    this.Parent.RemoveChild(this);
                    this.Parent.RefreshCount();
                    this.Parent.RefreshParents();
                }
            }
        }

        public void Delete()
        {
            if (IOHelper.SafeNativeMethods.MoveToRecycleBin(new string[] { this.Path }))
            {
                if (!File.Exists(this.Path) && !Directory.Exists(this.Path))
                {
                    this.Parent.RemoveChild(this);
                    this.Parent.RefreshCount();
                    this.Parent.RefreshParents();
                }
            }
        }

        private static void CanCallDeleteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject(e.OriginalSource);
            if (file == null)
                return;

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                return;

            e.CanExecute = !(file is VMRootFolder);
        }

        private static void CanCallShellCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject(e.OriginalSource);
            if (file == null)
                return;

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                return;

            if (command == ApplicationCommands.Properties
                || (command == ExploreCommand && (file.IsFile || !file.IsProtected))
                || (command == ApplicationCommands.Find && !file.IsFile && !file.IsProtected)
                || (command == OpenAsCommand && file.IsFile))
            {
                e.CanExecute = true;
                return;
            }

            e.CanExecute = ShellHelper.CanCallShellCommand(file.Path, command.Name);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        private static void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                throw new ArgumentNullException("e", "Command is not RoutedCommand");

            string path = file.Path;
            if (e.Command == ExploreCommand && file.IsFile)
                path = file.Parent.Path;

            ShellHelper.ShellExecute(path, command.Name.ToLowerInvariant(), new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
        }

        private static VMFile GetViewModelObject(object originalSource)
        {
            FrameworkElement element = originalSource as FrameworkElement;
            if (element == null)
                return null;

            return element.DataContext as VMFile;
        }

        #endregion Commands

    }
}
