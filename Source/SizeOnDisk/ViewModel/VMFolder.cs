using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPFByYourCommand;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFolder : VMFile
    {
        private uint clusterSize;

        public void PermanentDeleteAllSelectedFiles()
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
                this.FillChildList();
                this.RefreshCount();
                this.RefreshParents();
            }
        }

        public void DeleteAllSelectedFiles()
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
                this.FillChildList();
                this.RefreshCount();
                this.RefreshParents();
            }
        }


        #region fields

        private readonly Dispatcher _Dispatcher;
        protected Dispatcher Dispatcher { get => _Dispatcher; }

        #endregion fields

        #region constructor

        private object _myCollectionLock = new object();

        protected VMFolder(VMFolder parent, string name, string path, uint clusterSize, Dispatcher dispatcher)
            : base(parent, name, path)
        {
            this.clusterSize = clusterSize;
            FileCount = null;
            FolderCount = null;
            _Dispatcher = dispatcher;

            this.Childs = new DispatchObservableCollection<VMFile>();
            this.Folders = new DispatchObservableCollection<VMFolder>();

            /*dispatcher.BeginInvoke(new Action(() =>
            {
                BindingOperations.EnableCollectionSynchronization(this.Childs, _myCollectionLock);
                BindingOperations.EnableCollectionSynchronization(this.Folders, _myCollectionLock);
            }), DispatcherPriority.DataBind);*/
        }

        [DesignOnly(true)]
        internal VMFolder(VMFolder parent, string name)
            : base(parent, name)
        {
            FileCount = null;
            FolderCount = null;

            this.Childs = new DispatchObservableCollection<VMFile>();
            this.Folders = new DispatchObservableCollection<VMFolder>();
        }

        #endregion constructor

        #region properties

        public DispatchObservableCollection<VMFile> Childs { get; }

        public DispatchObservableCollection<VMFolder> Folders { get; }

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

        private bool _isTreeSelected;

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
                        this.SelectItem();
                    }
                    if (value && this.Path != null)
                    {
                        Thread thread = new Thread(() =>
                        {
                            this.FillChildList();

                            Parallel.ForEach(this.Childs, (T) => T.RefreshOnView());
                            //this.Childs.ToList().ForEach((T) => T.RefreshOnView());
                            //this.Childs.ToList().AsParallel().ForAll((T) => T.RefreshOnView());
                        });
                        thread.IsBackground = true;
                        thread.Priority = ThreadPriority.Highest;
                        thread.Start();
                    }
                }
            }
        }


        private VMFile selectedItem;
        public VMFile SelectedItem { get => selectedItem; set => SetProperty(ref selectedItem, value); }
        public uint ClusterSize { get => clusterSize; }

        #endregion properties

        #region functions

        public void RefreshCount()
        {
            if (Childs != null && Folders != null && !this.IsProtected)
            {
                this.FileCount = this.Childs.Sum(T => T.FileCount);
                this.FolderCount = this.Folders.Sum(T => T.FolderCount) + this.Folders.Count;
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

        private object _Lock = new object();

        public void FillChildList()
        {
            lock (_Lock)
            {
                try
                {
                    /*string[] childs;
                    childs = Directory.GetDirectories(this.Path);
                    VMFolder.RefreshChildsList<VMFolder>(childs, this.Childs, (p, q) => new VMFolder(this, p, q, this.clusterSize, this.Dispatcher));
                    childs = Directory.GetFiles(this.Path);
                    VMFolder.RefreshChildsList<VMFile>(childs, this.Childs, (p, q) => new VMFile(this, p, q));*/
                    List<VMFile> tmpChilds = this.Childs.ToList();
                    IEnumerable<LittleFileInfo> files = IOHelper.GetFiles(this.Path);
                    VMFile found = null;
                    foreach (LittleFileInfo fileInfo in files.OrderByDescending(T => T.IsFolder).ThenBy(T => T.Filename))
                    {
                        found = tmpChilds.FirstOrDefault(T => T.Name == fileInfo.Filename);
                        if (found == null)
                        {
                            if (fileInfo.IsFolder)
                            {
                                found = new VMFolder(this, fileInfo.Filename, System.IO.Path.Combine(fileInfo.Path, fileInfo.Filename), this.ClusterSize, this.Dispatcher);
                                this.Folders.Add(found as VMFolder);
                            }
                            else
                                found = new VMFile(this, fileInfo.Filename, System.IO.Path.Combine(fileInfo.Path, fileInfo.Filename));
                            this.Childs.Add(found);
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
                    }
                }

                catch (DirectoryNotFoundException)
                {

                }
                catch (UnauthorizedAccessException ex)
                {
                    this.IsProtected = true;
                }
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

                //Parallel.ForEach(this.Childs, (T) => T.RefreshOnView());
                //this.Childs.ToList().ForEach((T) => T.RefreshOnView());
                //this.Childs.ToList().AsParallel().ForAll((T) => T.RefreshOnView());
                this.FillChildList();

                if (this.IsTreeSelected)
                {
                    Parallel.ForEach(this.Childs, parallelOptions, (T) => T.RefreshOnView());
                    //this.Childs.ToList().ForEach((T) => T.RefreshOnView());
                    //this.Childs.ToList().AsParallel().WithCancellation(parallelOptions.CancellationToken).ForAll((T) => T.RefreshOnView());
                }

                //this.OnPropertyChanged(nameof(Folders));
                //this.OnPropertyChanged(nameof(Childs));
                Parallel.ForEach(this.Folders, parallelOptions, (T) => T.Refresh(parallelOptions));
                //Parallel.ForEach(this.Childs, parallelOptions, (T) => T.Refresh(parallelOptions));
                //this.Folders.ToList().ForEach((T) => T.Refresh(parallelOptions));
                /*foreach(VMFile vm in this.Childs)
                {
                    vm.Refresh(parallelOptions);
                }*/

                /*this.Childs.ToList().ForEach((T) =>
                {
                    Task task = new Task(() =>
                    {
                        T.Refresh(clusterSize, parallelOptions);
                    }, parallelOptions.CancellationToken, TaskCreationOptions.LongRunning);
                    task.Start();
                    task.Wait();
                });*/

                /*this.Childs.AsParallel().WithCancellation(parallelOptions.CancellationToken)
                    .WithMergeOptions(ParallelMergeOptions.NotBuffered).ForAll((T) =>
                    {
                        T.Refresh(clusterSize, parallelOptions);
                    });*/

                /*Task task = new Task(() =>
                {
                    try
                    {
                            this.Childs.ToList().ForEach((T) => T.Refresh(clusterSize, parallelOptions));
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            ExceptionBox.ShowException(ex);
                        });
                    }
                }, parallelOptions.CancellationToken, TaskCreationOptions.LongRunning);
                task.Start();
                task.Wait();*/
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
            this.RefreshCount();
            //this.RefreshLists();
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

        #region static functions

        /*private static void RefreshChildsList<T>(string[] childsPath, IList<VMFile> childs, Func<string, string, T> creator) where T : VMFile
        {
            List<T> filteredList;
            if (typeof(T) == typeof(VMFolder))
            {
                filteredList = childs.OfType<T>().ToList();
            }
            else
            {
                filteredList = childs.Where(p => p.GetType() == typeof(VMFile)).OfType<T>().ToList();
            }
            if (filteredList.Count > 0 || childsPath.Length > 0)
            {
                Dictionary<string, string> pathNameList = new Dictionary<string, string>();
                foreach (string child in childsPath)
                {
                    pathNameList.Add(System.IO.Path.GetFileName(child), child);
                }

                if (filteredList.Count > 0)
                {
                    List<VMFile> deleteChilds = new List<VMFile>();
                    foreach (VMFile child in filteredList)
                    {
                        if (!pathNameList.ContainsKey(child.Name))
                        {
                            deleteChilds.Add(child);
                        }
                    }
                    foreach (T child in deleteChilds)
                    {
                        childs.Remove(child);
                    }
                }
                if (pathNameList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> child in pathNameList)
                    {
                        if (!filteredList.Any(C => C.Name == child.Key))
                        {
                            childs.Add(creator(child.Key, child.Value));
                        }
                    }
                }
            }
        }*/

        #endregion static functions


    }
}
