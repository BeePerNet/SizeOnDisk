using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
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

namespace SizeOnDisk.ViewModel
{
    public class VMRootFolder : VMFolder
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
                this.Stop()?.WaitOne();
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

        #region properties

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
            : base(parent, name, name)
        {
            VMFolder newFolder = new VMFolder(this, "Folder1", "\\\\Root Folder\\Folder1");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            newFolder = new VMFolder(this, "Folder2", "\\\\Root Folder\\Folder2");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            VMFile newFile = new VMFile(this, "File1.txt", "\\\\Root Folder\\File1.txt");
            this.Childs.Add(newFile);
            newFile = new VMFile(this, "File2.42", "\\\\Root Folder\\File2.42");
            this.Childs.Add(newFile);
            this.RefreshCount();
            this._ExecutionState = TaskExecutionState.Designing;
        }

        internal VMRootFolder(VMRootHierarchy parent, string name, string path)
            : base(parent, name, path, (int)IOHelper.GetClusterSize(path))
        {
            HardDrivePath = System.IO.Path.GetPathRoot(path);
            this.SetInternalIsTreeSelected();
        }

        #endregion creator

        #region functions

        protected override void SelectItem()
        {
            (this.Parent as VMRootHierarchy).SelectedRootFolder = this;
        }
        protected override void SelectTreeItem(VMFolder folder)
        {
            (this.Parent as VMRootHierarchy).SelectedTreeItem = folder;
            (this.Parent as VMRootHierarchy).SelectedListItem = folder;
        }
        protected override void SelectListItem(VMFile selected)
        {
            (this.Parent as VMRootHierarchy).SelectedListItem = selected;
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


        private void RootExecuteTaskAsyncHighPriority(Action action)
        {
            ParallelOptions parallelOptions = GetParallelOptions();
            new Thread(() =>
            {
                ExecuteTask(action);
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            }.Start();
        }

        public override Task ExecuteTaskAsync(Action action, bool highpriority = false)
        {
            if (highpriority)
            {
                RootExecuteTaskAsyncHighPriority(action);
                return null;
            }
            else
            {
                ParallelOptions parallelOptions = GetParallelOptions();
                return Task.Run(() => ExecuteTask(action), parallelOptions.CancellationToken);
            }
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
                catch (Exception ex)
                {
                    ExceptionBox.ShowException(ex);
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
                ExceptionBox.ShowException(ex);
            }
        }

        public WaitHandle Stop()
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
                    ExecuteTaskAsync(() =>
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
                    return cts.Token.WaitHandle;
                }
            }
            catch (Exception ex)
            {
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
                    (this.Parent as VMRootHierarchy).RefreshIsRunning();
                }
            }
        }

        #endregion Task


    }
}
