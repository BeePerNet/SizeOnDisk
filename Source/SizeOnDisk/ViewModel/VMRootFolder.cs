using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SizeOnDisk.ViewModel
{
    public class VMRootFolder : VMFolder, IDisposable
    {
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
        internal VMRootFolder(VMRootHierarchy parent, string name, string path)
            : base(parent, name, path)
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

        internal VMRootFolder(VMRootHierarchy parent, string name, string path, Dispatcher dispatcher)
            : base(parent, name, path, IOHelper.GetClusterSize(path), dispatcher)
        {
            HardDrivePath = System.IO.Path.GetPathRoot(path);
            _isTreeSelected = true;
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
                this.ExecutionState = (parallelOptions.CancellationToken.IsCancellationRequested ? TaskExecutionState.Canceled : TaskExecutionState.Finished);
            }
        }

        #endregion functions

        #region Task

        private DispatcherTimer _Timer;
        private ParallelOptions _ParallelOptions;
        private CancellationTokenSource _CancellationTokenSource;
        private readonly object _lock = new object();

        private ParallelOptions GetParallelOptions()
        {
            if (_CancellationTokenSource != null)
            {
                if (!_CancellationTokenSource.Token.CanBeCanceled)
                {
                    _ParallelOptions = null;
                }
            }
            if (_ParallelOptions == null)
            {
                lock (_lock)
                {
                    if (_ParallelOptions == null)
                    {
                        ParallelOptions parallelOptions = new ParallelOptions();
                        _CancellationTokenSource = new CancellationTokenSource();
                        parallelOptions.CancellationToken = _CancellationTokenSource.Token;
                        _ParallelOptions = parallelOptions;
                    }
                }
            }
            return _ParallelOptions;
        }


        private void RootExecuteTaskAsyncHighPriority(Action<ParallelOptions> action)
        {
            ParallelOptions parallelOptions = GetParallelOptions();
            new Thread(() =>
            {
                ExecuteTask(action, parallelOptions);
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            }.Start();
        }

        protected override void ExecuteTaskAsync(Action<ParallelOptions> action, bool highpriority = false)
        {
            if (highpriority)
                RootExecuteTaskAsyncHighPriority(action);
            ParallelOptions parallelOptions = GetParallelOptions();
            Task.Run(() => ExecuteTask(action, parallelOptions), parallelOptions.CancellationToken);
        }

        [SuppressMessage("Microsoft.Design", "CA1031")]
        public void RefreshAsync()
        {
            this.Stop()?.Wait();

            this.ExecutionState = TaskExecutionState.Running;
            if (_Timer == null)
            {
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind, Dispatcher)
                {
                    Interval = new TimeSpan(0, 0, 1)
                };
                _Timer.Tick += new EventHandler(TimerTick);
            }

            ExecuteTaskAsync((parallelOptions) =>
            {
                try
                {
                    _Timer.Start();
                    this.Refresh(parallelOptions);
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
            });
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
                        if (!_ParallelOptions.CancellationToken.CanBeCanceled)
                            cts = null;
                        _CancellationTokenSource = null;
                        _ParallelOptions = null;
                    }
                    if (cts != null)
                    {
                        cts.Cancel(true);
                        Task task = Task.Run(() =>
                        {
                            try
                            {
                                cts.Token.WaitHandle.WaitOne();
                            }
                            catch (Exception ex)
                            {
                                ExceptionBox.ShowException(ex);
                            }
                            finally
                            {
                                this.ExecutionState = TaskExecutionState.Canceled;
                            }
                        });
                        return task;
                    }
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

        #region IDisposable

        private bool disposed = false;

        ~VMRootFolder()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            // process only if mananged and unmanaged resources have
            // not been disposed of.
            if (!this.disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                    this.Stop();

                    if (_CancellationTokenSource != null)
                    {
                        _CancellationTokenSource.Dispose();
                        _CancellationTokenSource = null;
                    }
                }

                // dispose unmanaged resources
                disposed = true;
            }
        }

        #endregion IDisposable
    }
}
