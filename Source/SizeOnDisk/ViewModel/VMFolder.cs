using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace SizeOnDisk.ViewModel
{
    public class VMFolder : VMFile
    {
        private readonly uint clusterSize;

        public void PermanentDeleteAllSelectedFiles()
        {
            ExecuteTask((parallelOptions) =>
            {
                VMFile[] files = this.Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.PermanentDelete(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file is VMFolder)
                            exists = Directory.Exists(file.Path);
                        else
                            exists = File.Exists(file.Path);
                        if (!exists)
                            deletedfiles.Add(file);
                    }
                    this.RefreshAfterCommand();
                }
            });
        }

        public void DeleteAllSelectedFiles()
        {
            ExecuteTask((parallelOptions) =>
            {
                VMFile[] files = this.Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.MoveToRecycleBin(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file is VMFolder)
                            exists = Directory.Exists(file.Path);
                        else
                            exists = File.Exists(file.Path);
                        if (!exists)
                            deletedfiles.Add(file);
                    }
                    this.RefreshAfterCommand();
                }
            });
        }


        #region fields

        private readonly Dispatcher _Dispatcher;
        protected Dispatcher Dispatcher { get => _Dispatcher; }

        #endregion fields

        #region constructor

        private readonly object _myCollectionLock = new object();

        protected VMFolder(VMFolder parent, string name, string path, uint clusterSize, Dispatcher dispatcher)
            : base(parent, name, path)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                BindingOperations.EnableCollectionSynchronization(this.Childs, _myCollectionLock);
                BindingOperations.EnableCollectionSynchronization(this.Folders, _myCollectionLock);
            }), DispatcherPriority.Send);

            this.clusterSize = clusterSize;
            _Dispatcher = dispatcher;
            FileTotal = null;
        }

        [DesignOnly(true)]
        internal VMFolder(VMFolder parent, string name, string path)
            : base(parent, name, path, true)
        {
            RefreshCount();
        }

        #endregion constructor

        #region properties

        public ObservableCollection<VMFile> Childs { get; } = new ObservableCollection<VMFile>();

        public ObservableCollection<VMFolder> Folders { get; } = new ObservableCollection<VMFolder>();

        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;

                    // Expand all the way up to the root.
                    if (_isExpanded && this.Parent != null)
                        this.Parent.IsExpanded = true;

                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public override string Extension
        {
            get
            {
                return string.Empty;
            }
        }



        private long? _FileTotal = 1;
        private long? _FolderTotal = null;

        public long? FileCount
        {
            get { return this.Childs?.Count - this.Folders?.Count; }
        }

        public override long? FileTotal
        {
            get { return _FileTotal; }
            protected set { SetProperty(ref _FileTotal, value); }
        }

        public override long? FolderTotal
        {
            get { return _FolderTotal; }
            protected set { SetProperty(ref _FolderTotal, value); }
        }







        protected virtual void SelectTreeItem(VMFolder folder)
        {
            this.Parent.SelectTreeItem(folder);
        }



        protected bool _isTreeSelected;

        public bool IsTreeSelected
        {
            get { return _isTreeSelected; }
            set
            {
                if (value != _isTreeSelected)
                {
                    _isTreeSelected = value;
                    this.OnPropertyChanged(nameof(IsTreeSelected));
                    if (_isTreeSelected && this.Parent != null)
                    {
                        this.Parent.IsExpanded = true;
                        this.SelectTreeItem(this);
                        this.SelectItem();
                    }
                    if (value && this.Path != null && _Dispatcher != null)
                    {
                        ExecuteTaskAsync((parallelOptions) =>
                        {
                            this.FillChildList();

                            Parallel.ForEach(this.Childs, parallelOptions, (T) => T.RefreshOnView());
                        }, true);
                    }
                }
            }
        }

        public void RefreshAfterCommand()
        {
            ExecuteTaskAsync((parallelOptions) =>
            {
                FillChildList(true);
                RefreshCount();
                RefreshParents();
            });
        }



        private VMFile selectedItem;
        public VMFile SelectedItem { get => selectedItem; set => SetProperty(ref selectedItem, value); }
        public uint ClusterSize { get => clusterSize; }

        #endregion properties

        #region functions

        public void RefreshCount()
        {
            this.OnPropertyChanged(nameof(this.FileCount));
            if (this.IsProtected)
            {
                this.FileTotal = null;
                this.FolderTotal = null;
                this.DiskSize = null;
                this.FileSize = null;
            }
            else
            {
                this.FileTotal = this.Childs.Sum(T => T.FileTotal);
                this.FolderTotal = this.Folders.Sum(T => T.FolderTotal) + this.Folders.Count;
                this.DiskSize = this.Childs.Sum(T => T.DiskSize);
                this.FileSize = this.Childs.Sum(T => T.FileSize);
            }
        }

        public void RefreshParents()
        {
            if (this.Parent != null)
            {
                this.Parent.RefreshCount();
                this.Parent.RefreshParents();
            }
        }

        public void FillChildList(bool refreshOnNew = false)
        {
            try
            {
                List<VMFile> tmpChilds = this.Childs.ToList();
                IEnumerable<LittleFileInfo> files = IOHelper.GetFiles(this.Path);
                VMFile found = null;
                foreach (LittleFileInfo fileInfo in files.OrderByDescending(T => T.IsFolder).ThenBy(T => T.FileName))
                {
                    found = tmpChilds.FirstOrDefault(T => T.Name == fileInfo.FileName);
                    if (found == null)
                    {
                        if (fileInfo.IsFolder)
                        {
                            found = new VMFolder(this, fileInfo.FileName, System.IO.Path.Combine(fileInfo.Path, fileInfo.FileName), this.ClusterSize, this.Dispatcher);
                            this.Folders.Add(found as VMFolder);
                            if (refreshOnNew)
                                (found as VMFolder).Refresh(new ParallelOptions());
                        }
                        else
                            found = new VMFile(this, fileInfo.FileName, System.IO.Path.Combine(fileInfo.Path, fileInfo.FileName));
                        this.Childs.Add(found);
                        if (refreshOnNew && this.Parent.IsTreeSelected)
                            this.RefreshOnView();
                    }
                    else
                    {
                        tmpChilds.Remove(found);
                    }
                    found.Refresh(fileInfo);
                }
                foreach (VMFile file in tmpChilds)
                {
                    if (file is VMFolder)
                        this.Folders.Remove(file as VMFolder);
                    this.Childs.Remove(file);
                    file.Dispose();
                }
            }
            catch (DirectoryNotFoundException)
            {

            }
            catch (UnauthorizedAccessException)
            {
                this.IsProtected = true;
            }
        }

        internal override void Refresh(LittleFileInfo fileInfo)
        {
            this.Attributes = fileInfo.Attributes;
        }

        [SuppressMessage("Microsoft.Design", "CA1031")]
        public virtual void Refresh(ParallelOptions parallelOptions)
        {
            if (Dispatcher == null || (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested))
                return;
            try
            {
                this.FillChildList();
                if (this.IsTreeSelected)
                {
                    ExecuteTaskAsync((po) =>
                    {
                        Parallel.ForEach(this.Childs, parallelOptions, (T) => T.RefreshOnView());
                    }, true);
                }
                Parallel.ForEach(this.Folders, parallelOptions, (T) => T.Refresh(parallelOptions));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
            if (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested)
                return;
            this.RefreshCount();
        }

        //TODO: This code was used before to detect if it was a reparse point
        //First idea was to create a new type of folder when found
        /*protected static VMFolder CreateVMFolder(VMFolder parent, string name, string path)
        {
            /*LittleFileInfo fileInfo = new Utilities.LittleFileInfo(path);
            FileAttributes attributes = fileInfo.Attributes;

            if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                //Parse the reparse point: new VMReparseFolder class to create
                ReparsePoint reparsePoint = new ReparsePoint(path);
                VMFolder folder = new VMFolderLink(parent, name, reparsePoint.Target, attributes);
                if (reparsePoint.Tag == ReparsePoint.TagType.None)
                {
                    folder.IsProtected = true;
                }
                return folder;
            }*/
        //return new VMFolder(parent, name, path);
        //}

        #endregion functions



    }
}
