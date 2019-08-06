using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            ExecuteTaskAsync(() =>
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

        #region fields

        private readonly Stopwatch _Runwatch = new Stopwatch();
        private long _HardDriveUsage;
        private long _HardDriveFree;
        private TaskExecutionState _ExecutionState = TaskExecutionState.Ready;

        #endregion fields

        public IEnumerable<VMRootFolder> DummyMe => new VMRootFolder[] { this };

        public new VMRootHierarchy Parent { get; }


        #region properties

        private VMFolder _SelectedTreeItem;

        public VMFolder SelectedTreeItem
        {
            get => _SelectedTreeItem;
            set => SetProperty(ref _SelectedTreeItem, value);
        }

        private VMFile _SelectedListItem;

        public VMFile SelectedListItem
        {
            get => _SelectedListItem;
            set => SetProperty(ref _SelectedListItem, value);
        }

        public string HardDrivePath { get; }

        public TimeSpan RunTime => _Runwatch.Elapsed;

        public long HardDriveUsage
        {
            get => _HardDriveUsage;
            protected set => SetProperty(ref _HardDriveUsage, value);
        }

        public long HardDriveFree
        {
            get => _HardDriveFree;
            protected set => SetProperty(ref _HardDriveFree, value);
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

            VMFolder newFolder = new VMFolder(this, "Blackbriar");
            Childs.Add(newFolder);
            Folders.Add(newFolder);
            VMFile newFile = new VMFile(this, "SubFile.txt", (ulong)1.44 * 1000 * 1024);
            newFolder.Childs.Add(newFile);
            newFile = new VMFile(this, "SubFile.txt", (ulong)10 * 1024 * 1000 * 1000);
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFolder = new VMFolder(this, "Threadstone");
            Childs.Add(newFolder);
            Folders.Add(newFolder);
            newFile = new VMFile(this, "Filezzz.txt", (uint)(1.44 * 1000 * 1024));
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFile = new VMFile(this, "Arecibo.txt", 1679);
            Childs.Add(newFile);

            newFile = new VMFile(this, "42.zip", 4503599626321920);
            Childs.Add(newFile);

            LogException(new Exception("Flagada"));

            RefreshCount();
            _ExecutionState = TaskExecutionState.Designing;

            SetInternalIsTreeSelected();
            SelectedTreeItem = this;
            SelectedListItem = this;
        }

        internal VMRootFolder(VMRootHierarchy parent, string name, string path)
            : base(null, name, path)
        {
            ClusterSize = IOHelper.GetClusterSize(path);
            Parent = parent;
            HardDrivePath = System.IO.Path.GetPathRoot(path);
            SetInternalIsTreeSelected();

            SelectedTreeItem = this;
            SelectedListItem = this;
        }

        public uint ClusterSize { get; }

        public bool IsDesign { get; } = false;


        #endregion creator

        #region functions

        protected override void SelectTreeItem(VMFolder folder)
        {
            SelectedTreeItem = folder;
            SelectedListItem = folder;
        }
        protected override void SelectListItem(VMFile selected)
        {
            SelectedListItem = selected;
        }

        public override void Refresh(ParallelOptions parallelOptions)
        {
            try
            {
                _Runwatch.Restart();

                DriveInfo info = new DriveInfo(HardDrivePath);
                HardDriveUsage = info.TotalSize - info.TotalFreeSpace;
                HardDriveFree = info.AvailableFreeSpace;

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

        private DispatcherTimer _Timer;
        private ParallelOptions _ParallelOptions;
        [SuppressMessage("Design", "CA2213")]
        private CancellationTokenSource _CancellationTokenSource;
        private readonly object _lock = new object();

        private ParallelOptions GetParallelOptions()
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


        public override Task ExecuteTaskAsync(Action action, bool highpriority = false)
        {
            ParallelOptions parallelOptions = GetParallelOptions();
            return Task.Factory.StartNew(new Action(() =>
                {
                    Exception ex = TaskHelper.SafeExecute(action);
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

            ExecuteTaskAsync(() =>
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
            }, true);
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
                    }, true);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                ExceptionBox.ShowException(ex);
            }
            return null;
        }

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



        #endregion Task




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

        public ObservableImmutableCollection<VMLog> Logs { get; } = new ObservableImmutableCollection<VMLog>();

        public void Log(VMLog log)
        {
            Logs.DoAdd((logs) => log);
        }

        public override VMRootFolder Root => this;



    }
}
