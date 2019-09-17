using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Exceptions;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
    public class VMRootFolder : VMFolder, IDisposable
    {
        public static readonly RoutedCommandEx RefreshCommand = new RoutedCommandEx("refresh", "loc:PresentationCore:ExceptionStringTable:RefreshText", "pack://application:,,,/SizeOnDisk;component/Icons/Refresh.png", typeof(VMRootFolder), new KeyGesture(Key.F5, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:RefreshKeyDisplayString"));
        public static readonly RoutedCommandEx StopCommand = new RoutedCommandEx("stop", "loc:PresentationCore:ExceptionStringTable:StopText", "pack://application:,,,/SizeOnDisk;component/Icons/StopHS.png", typeof(VMRootFolder), new KeyGesture(Key.Escape, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:StopKeyDisplayString"));
        public static readonly RoutedCommandEx CloseCommand = new RoutedCommandEx("close", "loc:PresentationCore:ExceptionStringTable:CloseText", "pack://application:,,,/SizeOnDisk;component/Icons/Close.png", typeof(VMRootFolder));
        public static readonly RoutedCommandEx SelectParentCommand = new RoutedCommandEx("selectparent", string.Empty, "pack://application:,,,/SizeOnDisk;component/Icons/Up.png", typeof(VMRootFolder));
        public static readonly RoutedCommandEx SelectPreviousCommand = new RoutedCommandEx("selectprevious", string.Empty, "pack://application:,,,/SizeOnDisk;component/Icons/Previous.png", typeof(VMRootFolder));
        public static readonly RoutedCommandEx SelectNextCommand = new RoutedCommandEx("selectnext", string.Empty, "pack://application:,,,/SizeOnDisk;component/Icons/Select.png", typeof(VMRootFolder));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
            {
                throw new ArgumentNullException(nameof(bindingCollection));
            }

            base.AddCommandModels(bindingCollection);
            bindingCollection.Add(new CommandBinding(RefreshCommand, CallRefreshCommand, CanCallRefreshCommand));
            bindingCollection.Add(new CommandBinding(StopCommand, CallStopCommand, CanCallStopCommand));
            bindingCollection.Add(new CommandBinding(CloseCommand, CallCloseCommand));
            bindingCollection.Add(new CommandBinding(SelectParentCommand, CallSelectParentCommand, CanCallSelectParentCommand));
            bindingCollection.Add(new CommandBinding(SelectPreviousCommand, CallSelectPreviousCommand, CanCallSelectPreviousCommand));
            bindingCollection.Add(new CommandBinding(SelectNextCommand, CallSelectNextCommand, CanCallSelectNextCommand));
        }




        private void CanCallSelectPreviousCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = history.Count > 0 && historyIndex > 0;
        }

        private void CallSelectPreviousCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (historyIndex > 0)
            {
                historyIndex--;
                LoadHistory();
            }
        }

        private void CanCallSelectNextCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = historyIndex < history.Count - 1;
        }

        private void CallSelectNextCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (historyIndex < history.Count - 1)
            {
                historyIndex++;
                LoadHistory();
            }
        }

        private void CanCallSelectParentCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = SelectedTreeItem != null && SelectedTreeItem.Parent != null;
        }

        private void CallSelectParentCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            SelectedTreeItem.Parent.IsTreeSelected = true;
        }

        private void CanCallStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = ExecutionState == TaskExecutionState.Running;
        }

        private void CallStopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Stop();
        }

        private void CallCloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Root.ExecuteTask(() =>
            {
                Stop()?.Wait();
                (Parent as VMRootHierarchy).RemoveRootFolder(this);
            }, true);
        }


        private void CanCallRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = ExecutionState != TaskExecutionState.Running;
        }

        private void CallRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            RefreshAsync();
        }

        private List<VMFolder> history = new List<VMFolder>();

        private bool isLoadingHistory = false;
        private int historyIndex = 0;

        public void LoadHistory()
        {
            isLoadingHistory = true;
            history[historyIndex].IsTreeSelected = true;
            isLoadingHistory = false;
        }

        private void AddHistory(VMFolder folder)
        {
            if (!isLoadingHistory && folder != null && (history.Count == 0 || history[historyIndex] != folder))
            {
                while (historyIndex < history.Count - 1)
                {
                    history.RemoveAt(history.Count - 1);
                }
                history.Add(folder);
                historyIndex = history.Count - 1;
            }
        }

        #region fields

        private readonly Stopwatch _Runwatch = new Stopwatch();
        private ulong _HardDriveUsage;
        private ulong _HardDriveFree;
        private ulong _HardDriveSize;
        private TaskExecutionState _ExecutionState = TaskExecutionState.Ready;

        #endregion fields

        #region properties

        public ObservableImmutableCollection<VMLog> Logs { get; } = new ObservableImmutableCollection<VMLog>();

        public override VMRootFolder Root => this;


        public uint ClusterSize { get; }

        public bool IsDesign { get; } = false;

        public IEnumerable<VMRootFolder> DummyMe => new VMRootFolder[] { this };

        public new VMRootHierarchy Parent { get; }

        private VMFile selectedItem;
        public VMFile SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }


        private VMFolder _SelectedTreeItem;

        public VMFolder SelectedTreeItem
        {
            get => _SelectedTreeItem;
            set
            {
                if (SetProperty(ref _SelectedTreeItem, value))
                {
                    AddHistory(value);
                }
            }
        }

        private VMFile _SelectedListItem;

        public VMFile SelectedListItem
        {
            get => _SelectedListItem;
            set => SetProperty(ref _SelectedListItem, value);
        }

        public string HardDrivePath { get; }

        public TimeSpan RunTime => _Runwatch.Elapsed;

        public ulong HardDriveUsage
        {
            get => _HardDriveUsage;
            protected set => SetProperty(ref _HardDriveUsage, value);
        }

        public ulong HardDriveFree
        {
            get => _HardDriveFree;
            protected set => SetProperty(ref _HardDriveFree, value);
        }

        public ulong HardDriveSize
        {
            get => _HardDriveSize;
            protected set => SetProperty(ref _HardDriveSize, value);
        }

        #endregion properties

        #region creator

        [DesignOnly(true)]
        public VMRootFolder() : this(null) { }


        [DesignOnly(true)]
        internal VMRootFolder(VMRootHierarchy parent)
            : base(null, "\\\\CanNotBeDrive\\Groot Folder")
        {
            IsDesign = true;
            Parent = parent;
            HardDrivePath = "Drive 1";

            IsExpanded = true;
            IsTreeSelected = true;

            VMFolder newFolder = new VMFolder(this, "Blackbriar");
            Childs.Add(newFolder);
            Folders.Add(newFolder);
            VMFile newFile = new VMFile(this, "SubFile.txt", (long)1.44 * 1000 * 1024);
            newFolder.Childs.Add(newFile);
            newFile = new VMFile(this, "SubFile.txt", (long)10 * 1024 * 1000 * 1000);
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFolder = new VMFolder(this, "Threadstone");
            Childs.Add(newFolder);
            Folders.Add(newFolder);
            newFolder.IsSelected = true;
            newFile = new VMFile(this, "Filezzz.txt", (uint)(1.44 * 1000 * 1024));
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFile = new VMFile(this, "Arecibo.txt", 1679);
            Childs.Add(newFile);

            newFile = new VMFile(this, "42.zip", 4503599626321920);
            Childs.Add(newFile);

            newFile = new VMFile(this, "64.lnk", 1339);
            Childs.Add(newFile);

            LogException(new Exception("Flagada"));

            RefreshCount();
            _ExecutionState = TaskExecutionState.Designing;


            HardDriveUsage = DiskSize ?? 0;
            HardDriveSize = 1000202039296000000;
            HardDriveFree = HardDriveSize - HardDriveUsage;
        }

        internal VMRootFolder(VMRootHierarchy parent, string name, string path)
            : base(null, name, path)
        {
            ClusterSize = ShellHelper.GetClusterSize(path);
            Parent = parent;
            HardDrivePath = System.IO.Path.GetPathRoot(path);
            IsExpanded = true;
            IsTreeSelected = true;
        }

        #endregion creator

        #region functions

        public void Log(VMLog log)
        {
            Logs.DoAdd((logs) => log);
        }

        public override VMFile FindVMFile(string path)
        {
            if (string.Equals(path, this.Path, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }
            else if (path.StartsWith("\\", StringComparison.Ordinal))
            {
                return base.FindVMFile(path);
            }
            else if (path.StartsWith(this.Path, StringComparison.CurrentCultureIgnoreCase))
            {
                path = path.Remove(0, this.Path.Length);
                if (!path.StartsWith("\\", StringComparison.Ordinal))
                    path = "\\" + path;
                return base.FindVMFile(path);
            }
            else
            {
                return Parent.FindVMFile(path);
            }
        }

        public override void RefreshCount()
        {
            base.RefreshCount();

            if (!IsDesign)
            {
                DriveInfo info = new DriveInfo(HardDrivePath);
                HardDriveUsage = (ulong)info.TotalSize - (ulong)info.TotalFreeSpace;
                HardDriveFree = (ulong)info.AvailableFreeSpace;
                HardDriveSize = (ulong)info.TotalSize;
            }
        }

        public override void Refresh(ParallelOptions parallelOptions)
        {
            try
            {
                _Runwatch.Restart();

                this.RefreshCount();

                base.Refresh(parallelOptions);
            }
            finally
            {
                _Runwatch.Stop();
                OnPropertyChanged(nameof(RunTime));
                if (parallelOptions != null && parallelOptions.CancellationToken != null)
                {
                    ExecutionState = (parallelOptions.CancellationToken.IsCancellationRequested ? TaskExecutionState.Canceled : TaskExecutionState.Finished);
                }
                else
                {
                    ExecutionState = TaskExecutionState.Unknown;
                }
            }
        }

        #endregion functions

        #region Task

        [EnumDataType(typeof(TaskExecutionState))]
        public TaskExecutionState ExecutionState
        {
            get => _ExecutionState;
            private set
            {
                if (_ExecutionState != value)
                {
                    _ExecutionState = value;
                    OnPropertyChanged(nameof(ExecutionState));
                    Parent.RefreshIsRunning();
                }
            }
        }

        private VMViewMode viewMode = VMViewMode.Details;
        public VMViewMode ViewMode { get => viewMode; set => SetProperty(ref viewMode, value); }


        private bool _IsPropertiesVisible;
        public bool IsPropertiesVisible { get => _IsPropertiesVisible; set => SetProperty(ref _IsPropertiesVisible, value); }


        private DispatcherTimer _Timer;
        private ParallelOptions _ParallelOptions;
        [SuppressMessage("Design", "CA2213")]
        private CancellationTokenSource _CancellationTokenSource;
        private readonly object _lock = new object();

        public ParallelOptions GetParallelOptions()
        {
            if (_CancellationTokenSource != null && _CancellationTokenSource.Token != null && !_CancellationTokenSource.Token.CanBeCanceled)
            {
                _ParallelOptions = null;
            }

            if (_ParallelOptions == null)
            {
                lock (_lock)
                {
                    if (_ParallelOptions == null)
                    {
                        if (_CancellationTokenSource != null)
                        {
                            _CancellationTokenSource.Dispose();
                        }

                        _ParallelOptions = new ParallelOptions();
                        _CancellationTokenSource = new CancellationTokenSource();
                        _ParallelOptions.CancellationToken = _CancellationTokenSource.Token;
                    }
                }
            }
            return _ParallelOptions;
        }

        public Exception ExecuteTask(Action action, bool showException)
        {
            Exception ex = TaskHelper.SafeExecute(action, showException);
            if (ex != null)
            {
                LogException(ex);
            }
            return ex;
        }

        public void ExecuteTaskAsyncByDispatcher(Action action, bool showException, Dispatcher dispatcher)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                Exception ex = TaskHelper.SafeExecute(action, showException);
                if (ex != null)
                {
                    LogException(ex);
                }
            }));
        }

        public Task ExecuteTaskAsync(Action action, bool showException, bool highpriority)
        {
            ParallelOptions parallelOptions = GetParallelOptions();
            return Task.Factory.StartNew(new Action(() =>
                {
                    Exception ex = TaskHelper.SafeExecute(action, showException);
                    if (ex != null)
                    {
                        LogException(ex);
                    }
                }),
                parallelOptions.CancellationToken,
                (highpriority ? TaskCreationOptions.LongRunning : TaskCreationOptions.None) | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        public void RefreshAsync()
        {
            ExecutionState = TaskExecutionState.Running;
            if (_Timer == null)
            {
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind, Application.Current.Dispatcher)
                {
                    Interval = new TimeSpan(0, 0, 1)
                };
                _Timer.Tick += new EventHandler(TimerTick);
            }

            Root.ExecuteTaskAsync(() =>
            {
                try
                {
                    _Timer.Start();
                    Refresh(GetParallelOptions());
                    ExecutionState = TaskExecutionState.Finished;
                }
                finally
                {
                    _Timer.Stop();
                }
            }, false, false);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            try
            {
                OnPropertyChanged(nameof(RunTime));
                RefreshCount();
            }
            catch (Exception ex)
            {
                LogException(ex);
                ExceptionBox.ShowException(ex);
            }
        }

        public Task Stop()
        {
            try
            {
                if (_CancellationTokenSource != null && _CancellationTokenSource.Token.CanBeCanceled)
                {
                    ExecutionState = TaskExecutionState.Canceling;

                    CancellationTokenSource cts;
                    lock (_lock)
                    {
                        cts = _CancellationTokenSource;
                        if (cts.Token.CanBeCanceled)
                        {
                            cts.Cancel(true);
                        }

                        _CancellationTokenSource = null;
                        _ParallelOptions = null;
                    }
                    return ExecuteTaskAsync(() =>
                    {
                        try
                        {
                            cts.Token.WaitHandle.WaitOne();
                        }
                        finally
                        {
                            ExecutionState = TaskExecutionState.Canceled;
                        }
                    }, true, true);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                ExceptionBox.ShowException(ex);
            }
            return null;
        }


        #endregion Task

        #region IDisposable

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();
                if (_CancellationTokenSource != null)
                {
                    _CancellationTokenSource.Dispose();
                }
            }
            // free native resources
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable


    }
}
