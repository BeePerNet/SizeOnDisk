using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using SizeOnDisk.Utilities;

namespace SizeOnDisk.ViewModel
{
    public class VMRootFolder : VMFolder, IDisposable
    {
        #region fields

        private Stopwatch _Runwatch = new Stopwatch();
        long _HardDriveUsage;
        long _HardDriveFree;
        TaskExecutionState _ExecutionState = TaskExecutionState.Ready;
        private string _HardDrivePath;

        #endregion fields

        #region properties

        public string HardDrivePath
        {
            get { return _HardDrivePath; }
        }

        public TimeSpan Runtime
        {
            get
            {
                return _Runwatch.Elapsed;
            }
        }

        public long HardDriveUsage
        {
            get { return _HardDriveUsage; }
            protected set
            {
                if (_HardDriveUsage != value)
                {
                    _HardDriveUsage = value;
                    this.OnPropertyChanged("HardDriveUsage");
                }
            }
        }

        public long HardDriveFree
        {
            get { return _HardDriveFree; }
            protected set
            {
                if (_HardDriveFree != value)
                {
                    _HardDriveFree = value;
                    this.OnPropertyChanged("HardDriveFree");
                }
            }
        }

        #endregion properties

        #region creator

        internal VMRootFolder(VMRootHierarchy parent, string name, string path, Dispatcher dispatcher)
            : base(parent, name, path, dispatcher)
        {
            _HardDrivePath = System.IO.Path.GetPathRoot(path);
            this.IsExpanded = true;
            this.IsSelected = true;
        }

        #endregion creator

        #region functions

        protected override void SelectItem()
        {
            (this.Parent as VMRootHierarchy).SelectedRootFolder = this;
        }

        private void Refresh(ParallelOptions parallelOptions)
        {
            try
            {
                this.ExecutionState = TaskExecutionState.Running;
                _Runwatch.Restart();
                this.OnPropertyChanged("RunTime");

                DriveInfo info = new DriveInfo(_HardDrivePath);
                this.HardDriveUsage = info.TotalSize - info.TotalFreeSpace;
                this.HardDriveFree = info.AvailableFreeSpace;

                base.Refresh(IOHelper.GetClusterSize(this.Path), parallelOptions);
            }
            finally
            {
                _Runwatch.Stop();
                this.OnPropertyChanged("RunTime");

                this.ExecutionState = (parallelOptions.CancellationToken.IsCancellationRequested ? TaskExecutionState.Canceled : TaskExecutionState.Finished);
            }
        }

        #endregion functions

        #region Task

        private DispatcherTimer _Timer;
        private Task _Task;
        private CancellationTokenSource _CancellationTokenSource;

        [SuppressMessage("Microsoft.Design", "CA1031")]
        public void RefreshAsync()
        {
            if (_Timer == null)
            {
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind);
                _Timer.Interval = new TimeSpan(0, 0, 1);
                _Timer.Tick += new EventHandler(_timer_Tick);
            }
            this.StopAsync();
            ParallelOptions parallelOptions = new ParallelOptions();
            _CancellationTokenSource = new CancellationTokenSource();
            parallelOptions.CancellationToken = _CancellationTokenSource.Token;
            Action action = new Action(delegate ()
            {
                try
                {
                    try
                    {
                        _Timer.Start();
                        this.Refresh(parallelOptions);
                    }
                    finally
                    {
                        _Timer.Stop();
                        _Task = null;
                        _CancellationTokenSource = null;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _Dispatcher.Invoke((ThreadStart)delegate
                    {
                        ExceptionBox.ShowException(ex);
                    });
                }
            });
            _Task = new Task(action, parallelOptions.CancellationToken);
            _Task.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            this.OnPropertyChanged("RunTime");
            this.RefreshCount();
        }

        public void Stop()
        {
            if (_Task != null && _CancellationTokenSource != null)
            {
                StopAsync();
                if (_Task != null)
                {
                    _Task.Wait();
                }
            }
        }

        public void StopAsync()
        {
            if (_CancellationTokenSource != null && !_CancellationTokenSource.IsCancellationRequested)
            {
                _CancellationTokenSource.Cancel();
            }
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
                    this.OnPropertyChanged("ExecutionState");
                    (this.Parent as VMRootHierarchy).RefreshIsRunning();
                    CommandManager.InvalidateRequerySuggested();
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

        public void Dispose()
        {
            Dispose(true);
            // tell the GC that the Finalize process no longer needs
            // to be run for this object.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposeManagedResources)
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
                    if (_Task != null)
                    {
                        _Task.Dispose();
                        _Task = null;
                    }
                }

                // dispose unmanaged resources
                disposed = true;
            }
        }

        #endregion IDisposable
    }
}
