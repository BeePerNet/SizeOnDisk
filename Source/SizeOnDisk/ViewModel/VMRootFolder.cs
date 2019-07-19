﻿using SizeOnDisk.Shell;
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

        private Stopwatch _Runwatch = new Stopwatch();
        long _HardDriveUsage;
        long _HardDriveFree;
        TaskExecutionState _ExecutionState = TaskExecutionState.Running;
        private readonly string _HardDrivePath;

        #endregion fields

        #region properties

        public string HardDrivePath
        {
            get { return _HardDrivePath; }
        }

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
            VMFolder newFolder = new VMFolder(this, "Folder 1", "\\\\Root Folder\\Folder 1");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            newFolder = new VMFolder(this, "Folder 2", "\\\\Root Folder\\Folder 2");
            this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            VMFile newFile = new VMFile(this, "File 1", "\\\\Root Folder\\File 1");
            this.Childs.Add(newFile);
            this.RefreshCount();
            this._ExecutionState = TaskExecutionState.Designing;
        }

        internal VMRootFolder(VMRootHierarchy parent, string name, string path, Dispatcher dispatcher)
            : base(parent, name, path, IOHelper.GetClusterSize(path), dispatcher)
        {
            this.ExecutionState = TaskExecutionState.Running;
            _HardDrivePath = System.IO.Path.GetPathRoot(path);
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
        protected override void SelectListItem(VMFile file)
        {
            (this.Parent as VMRootHierarchy).SelectedListItem = file;
        }



        public override void Refresh(ParallelOptions parallelOptions)
        {
            try
            {
                this.ExecutionState = TaskExecutionState.Running;
                _Runwatch.Restart();
                this.OnPropertyChanged(nameof(RunTime));

                DriveInfo info = new DriveInfo(_HardDrivePath);
                this.HardDriveUsage = info.TotalSize - info.TotalFreeSpace;
                this.HardDriveFree = info.AvailableFreeSpace;

                this.IsExpanded = true;
                this.IsTreeSelected = true;

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
        private Task _Task;
        private CancellationTokenSource _CancellationTokenSource;

        [SuppressMessage("Microsoft.Design", "CA1031")]
        public void RefreshAsync()
        {
            this.ExecutionState = TaskExecutionState.Running;
            if (_Timer == null)
            {
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind)
                {
                    Interval = new TimeSpan(0, 0, 1)
                };
                _Timer.Tick += new EventHandler(_timer_Tick);
            }
            this.StopAsync();
            ParallelOptions parallelOptions = new ParallelOptions();
            _CancellationTokenSource = new CancellationTokenSource();
            parallelOptions.CancellationToken = _CancellationTokenSource.Token;
            _Task = new Task(() =>
            {
                try
                {
                    try
                    {
                        _Timer.Start();
                        //this.RefreshOnView();
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
                    ExceptionBox.ShowException(ex);
                }
            }, parallelOptions.CancellationToken, TaskCreationOptions.LongRunning);
            _Task.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(RunTime));
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
            if (_Task != null && _Task.Status == TaskStatus.Running)
            {
                if (_CancellationTokenSource != null && !_CancellationTokenSource.IsCancellationRequested)
                {
                    _CancellationTokenSource.Cancel();
                }
                this.ExecutionState = TaskExecutionState.Canceled;
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
