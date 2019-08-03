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
    public class VMRootFolder : VMFolder, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx RefreshCommand = new RoutedCommandEx("refresh", "loc:PresentationCore:ExceptionStringTable:RefreshText", "pack://application:,,,/SizeOnDisk;component/Icons/Refresh.png", typeof(VMRootFolder), new KeyGesture(Key.F5, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:RefreshKeyDisplayString"));
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx StopCommand = new RoutedCommandEx("stop", "loc:PresentationCore:ExceptionStringTable:StopText", "pack://application:,,,/SizeOnDisk;component/Icons/StopHS.png", typeof(VMRootFolder), new KeyGesture(Key.Escape, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:StopKeyDisplayString"));
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx CloseCommand = new RoutedCommandEx("close", "loc:PresentationCore:ExceptionStringTable:CloseText", "pack://application:,,,/SizeOnDisk;component/Icons/Close.png", typeof(VMRootFolder));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException(nameof(bindingCollection));
            base.AddCommandModels(bindingCollection);
            bindingCollection.Add(new CommandBinding(RefreshCommand, CallRefreshCommand, CanCallRefreshCommand));
            bindingCollection.Add(new CommandBinding(StopCommand, CallStopCommand, CanCallStopCommand));
            bindingCollection.Add(new CommandBinding(CloseCommand, CallCloseCommand));
        }

        private void CanCallStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.ExecutionState == TaskExecutionState.Running;
        }

        private void CallStopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.Stop();
        }

        private void CallCloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ExecuteTaskAsync(() =>
            {
                this.Stop()?.Wait();
                (this.Parent as VMRootHierarchy).RemoveRootFolder(this);
            }, true);
        }


        private void CanCallRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.ExecutionState != TaskExecutionState.Running;
        }

        private void CallRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.RefreshAsync();
        }

        #region fields

        private readonly Stopwatch _Runwatch = new Stopwatch();
        private long _HardDriveUsage;
        private long _HardDriveFree;
        private TaskExecutionState _ExecutionState = TaskExecutionState.Ready;

        #endregion fields

        public IEnumerable<VMRootFolder> DummyMe
        {
            get
            {
                return new VMRootFolder[] { this };
            }
        }

        new public VMRootHierarchy Parent { get; }


        #region properties

        VMFolder _SelectedTreeItem;

        public VMFolder SelectedTreeItem
        {
            get { return _SelectedTreeItem; }
            set { SetProperty(ref _SelectedTreeItem, value); }
        }

        VMFile _SelectedListItem;

        public VMFile SelectedListItem
        {
            get { return _SelectedListItem; }
            set { SetProperty(ref _SelectedListItem, value); }
        }

        public string HardDrivePath { get; }

        public TimeSpan RunTime
        {
            get
            {
                return _Runwatch.Elapsed;
            }
        }

        public long HardDriveUsage
        {
            get { return _HardDriveUsage; }
            protected set { SetProperty(ref _HardDriveUsage, value); }
        }

        public long HardDriveFree
        {
            get { return _HardDriveFree; }
            protected set { SetProperty(ref _HardDriveFree, value); }
        }

        #endregion properties

        #region creator

        [DesignOnly(true)]
        internal VMRootFolder(VMRootHierarchy parent, string name)
            : base(null, name, name)
        {
            this.Parent = parent;

            VMFolder newFolder = new VMFolder(this, "Blackbriar", "\\\\Root Folder\\Folder1");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            VMFile newFile = new VMFile(this, "SubFile.txt", "\\\\Root Folder\\Folder 2\\SubFile.txt", (ulong)1.44 * 1000 * 1024);
            newFolder.Childs.Add(newFile);
            newFile = new VMFile(this, "SubFile.txt", "\\\\Root Folder\\Folder 2\\SubFile.txt", (ulong)10 * 1024 * 1000 * 1000);
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFolder = new VMFolder(this, "Threadstone", "\\\\Root Folder\\Folder2");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            newFile = new VMFile(this, "Filezzz.txt", "\\\\Root Folder\\Folder 2\\Filezzz.txt", (uint)(1.44 * 1000 * 1024));
            newFolder.Childs.Add(newFile);
            newFolder.RefreshCount();

            newFile = new VMFile(this, "Arecibo.txt", "\\\\Root Folder\\Arecibo.txt", 1679);
            this.Childs.Add(newFile);

            newFile = new VMFile(this, "42.zip", "\\\\Root Folder\\42.zip", 4503599626321920);
            this.Childs.Add(newFile);

            this.RefreshCount();
            this._ExecutionState = TaskExecutionState.Designing;

            this.SetInternalIsTreeSelected();
            this.SelectedTreeItem = this;
            this.SelectedListItem = this;
        }

        internal VMRootFolder(VMRootHierarchy parent, string name, string path)
            : base(null, name, path, (int)IOHelper.GetClusterSize(path))
        {
            this.Parent = parent;
            HardDrivePath = System.IO.Path.GetPathRoot(path);
            this.SetInternalIsTreeSelected();

            this.SelectedTreeItem = this;
            this.SelectedListItem = this;
        }

        #endregion creator

        #region functions

        protected override void SelectTreeItem(VMFolder folder)
        {
            this.SelectedTreeItem = folder;
            this.SelectedListItem = folder;
        }
        protected override void SelectListItem(VMFile selected)
        {
            this.SelectedListItem = selected;
        }

        public override void Refresh(ParallelOptions parallelOptions)
        {
            try
            {
                _Runwatch.Restart();

                DriveInfo info = new DriveInfo(HardDrivePath);
                this.HardDriveUsage = info.TotalSize - info.TotalFreeSpace;
                this.HardDriveFree = info.AvailableFreeSpace;

                base.Refresh(parallelOptions);
            }
            finally
            {
                _Runwatch.Stop();
                this.OnPropertyChanged(nameof(RunTime));
                if (parallelOptions != null && parallelOptions.CancellationToken != null)
                    this.ExecutionState = (parallelOptions.CancellationToken.IsCancellationRequested ? TaskExecutionState.Canceled : TaskExecutionState.Finished);
                else
                    this.ExecutionState = TaskExecutionState.Unknown;
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
                _ParallelOptions = null;

            if (_ParallelOptions == null)
            {
                lock (_lock)
                {
                    if (_ParallelOptions == null)
                    {
                        if (_CancellationTokenSource != null)
                            _CancellationTokenSource.Dispose();

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
                        LogException(ex);
                }),
                parallelOptions.CancellationToken,
                (highpriority ? TaskCreationOptions.LongRunning : TaskCreationOptions.None) | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        public void RefreshAsync()
        {
            this.ExecutionState = TaskExecutionState.Running;
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
                    this.Refresh(this.GetParallelOptions());
                    this.ExecutionState = TaskExecutionState.Finished;
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
                this.OnPropertyChanged(nameof(RunTime));
                this.RefreshCount();
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
                    this.ExecutionState = TaskExecutionState.Canceling;

                    CancellationTokenSource cts;
                    lock (_lock)
                    {
                        cts = _CancellationTokenSource;
                        if (cts.Token.CanBeCanceled)
                            cts.Cancel(true);
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
                            this.ExecutionState = TaskExecutionState.Canceled;
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
            get
            {
                return _ExecutionState;
            }
            private set
            {
                if (_ExecutionState != value)
                {
                    _ExecutionState = value;
                    this.OnPropertyChanged(nameof(ExecutionState));
                    this.Parent.RefreshIsRunning();
                }
            }
        }

        private VMViewMode viewMode = VMViewMode.Details;
        public VMViewMode ViewMode { get => viewMode; set => SetProperty(ref viewMode, value); }


        #endregion Task




        bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                this.Stop();
                if (_CancellationTokenSource != null)
                    _CancellationTokenSource.Dispose();
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

        public override void Log(VMLog log)
        {
            Logs.DoAdd((logs) => log);
        }


    }
}
