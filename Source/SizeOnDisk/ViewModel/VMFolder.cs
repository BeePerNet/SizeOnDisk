using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPFByYourCommand;

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

        protected VMFolder(VMFolder parent, string name, string path, uint clusterSize, Dispatcher dispatcher)
            : base(parent, name, path)
        {
            this.clusterSize = clusterSize;
            FileCount = null;
            FolderCount = null;
            _Dispatcher = dispatcher;
        }

        [DesignOnly(true)]
        internal VMFolder(VMFolder parent, string name)
            : base(parent, name)
        {
            FileCount = null;
            FolderCount = null;
        }

        #endregion constructor

        #region properties

        public Collection<VMFile> Childs { get; protected set; }

        public VMFolder[] Folders { get; protected set; }

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
                        new Thread(() =>
                        {
                            if (this.Childs == null)
                                this.FillChildList();

                            Parallel.ForEach(this.Childs, (T) => T.RefreshOnView());
                            //this.Childs.ToList().ForEach((T) => T.RefreshOnView());
                            //this.Childs.ToList().AsParallel().ForAll((T) => T.RefreshOnView());
                        }).Start();
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
                this.FolderCount = this.Folders.Sum(T => T.FolderCount) + this.Folders.Length;
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

        //private object _Lock = new object();

        public void FillChildList()
        {
            try
            {
                //lock (_Lock)
                {
                    if (this.Childs == null)
                        this.Childs = new Collection<VMFile>();
                    /*string[] childs;
                    childs = Directory.GetDirectories(this.Path);
                    VMFolder.RefreshChildsList<VMFolder>(childs, this.Childs, (p, q) => new VMFolder(this, p, q, this.clusterSize, this.Dispatcher));
                    childs = Directory.GetFiles(this.Path);
                    VMFolder.RefreshChildsList<VMFile>(childs, this.Childs, (p, q) => new VMFile(this, p, q));*/
                    VMFile[] tmpChilds = this.Childs.ToArray();
                    Collection<VMFile> result = new Collection<VMFile>();
                    VMFile found = null;

                    LittleFileInfo[] files = IOHelper.GetFiles(this.Path);
                    foreach (LittleFileInfo fileInfo in files)
                    {
                        found = tmpChilds.FirstOrDefault(T => T.Name == fileInfo.Filename);
                        if (found == null)
                        {
                            if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                                found = new VMFolder(this, fileInfo.Filename, System.IO.Path.Combine(fileInfo.Path, fileInfo.Filename), this.ClusterSize, this.Dispatcher);
                            else
                                found = new VMFile(this, fileInfo.Filename, System.IO.Path.Combine(fileInfo.Path, fileInfo.Filename));
                        }
                        found.Refresh(fileInfo);
                        result.Add(found);
                    }
                    this.Childs = result;
                }
            }
            catch (DirectoryNotFoundException)
            {

            }
            catch (UnauthorizedAccessException ex)
            {
                this.IsProtected = true;
            }
            this.Folders = this.Childs.OfType<VMFolder>().ToArray();
            this.OnPropertyChanged(nameof(Childs));
            this.OnPropertyChanged(nameof(Folders));
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
