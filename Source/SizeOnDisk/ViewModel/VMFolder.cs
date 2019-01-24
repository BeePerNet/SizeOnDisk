using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SizeOnDisk.Utilities;
using System.Windows.Threading;
using System.Threading;

namespace SizeOnDisk.ViewModel
{
    public class VMFolder : VMFile
    {
        #region fields

        protected NotifyCollection<VMFile> _InternalChilds = new NotifyCollection<VMFile>();
        protected Collection<VMFolder> _InternalFolders;
        protected Dispatcher _Dispatcher;

        #endregion fields

        #region constructor

        protected VMFolder(VMFolder parent, string name, string path, Dispatcher dispatcher)
            : base(parent, name, path)
        {
            FileCount = null;
            FolderCount = null;
            _Dispatcher = dispatcher;
        }

        #endregion constructor

        #region properties

        public Collection<VMFile> Childs
        {
            get { return _InternalChilds; }
        }

        public Collection<VMFolder> Folders
        {
            get { return _InternalFolders; }
        }

        public override bool IsFile
        {
            get { return false; }
        }

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

                    this.OnPropertyChanged("IsExpanded");
                }
            }
        }

        public override bool IsSelected
        {
            get { return base.IsSelected; }
            set
            {
                if (value != base.IsSelected)
                {
                    base.IsSelected = value;
                    if (value && this._InternalChilds != null)
                    {
                        new Thread(() =>
                        {
                            _InternalChilds.ToList().ForEach((T) => T.RefreshOnView());
                            //Parallel.ForEach(_InternalChilds, (T) => T.RefreshOnView());
                        }).Start();
                    }
                }
            }
        }

        #endregion properties

        #region functions

        internal void RemoveChild(VMFile file)
        {
            this._InternalChilds.Remove(file);

            this.RefreshLists();
        }

        private void RefreshLists()
        {
            _Dispatcher.BeginInvoke((Action)(() =>
            {
                this._InternalChilds.OnCollectionChanged();
            }));
            this._InternalFolders = new Collection<VMFolder>(_InternalChilds.OfType<VMFolder>().OrderBy(T => T.Name).ToList());
            this.OnPropertyChanged("Folders");
        }

        public void RefreshCount()
        {
            this.FileCount = this._InternalChilds.Sum(T => T.FileCount);
            this.FolderCount = this.Folders.Sum(T => T.FolderCount) + this.Folders.Count;
            this.DiskSize = _InternalChilds.Sum(T => T.DiskSize);
            this.FileSize = _InternalChilds.Sum(T => T.FileSize);
        }

        public void RefreshParents()
        {
            if (this.Parent != null)
            {
                this.Parent.RefreshCount();
                this.Parent.RefreshParents();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031")]
        public override void Refresh(uint clusterSize, ParallelOptions parallelOptions)
        {
            if (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested)
                return;
            try
            {
                string[] childs;
                childs = Directory.GetDirectories(this.Path);
                VMFolder.RefreshChildsList<VMFolder>(childs, _InternalChilds, (p, q) => new VMFolder(this, p, q, this._Dispatcher));
                childs = Directory.GetFiles(this.Path);
                VMFolder.RefreshChildsList<VMFile>(childs, _InternalChilds, (p, q) => new VMFile(this, p, q));

                this.RefreshLists();

                if (this.IsSelected)
                {
                    //Parallel.ForEach(_InternalChilds, parallelOptions, (T) => T.RefreshOnView());
                    _InternalChilds.ToList().ForEach((T) => T.RefreshOnView());
                }

                Parallel.ForEach(_InternalChilds, parallelOptions, (T) => T.Refresh(clusterSize, parallelOptions));

                this.RefreshCount();
            }
            catch (OperationCanceledException)
            {
            }
            catch (UnauthorizedAccessException)
            {
                this.IsProtected = true;
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(this.Path, ex);
            }
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

        private static void RefreshChildsList<T>(string[] childsPath, IList<VMFile> childs, Func<string, string, T> creator) where T : VMFile
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
        }

        #endregion static functions


    }
}
