using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFByYourCommand.Exceptions;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFolder : VMFile
    {
        public void PermanentDeleteAllSelectedFiles()
        {
            ExecuteTaskAsync(() =>
            {
                VMFile[] files = this.Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.PermanentDelete(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file.IsFile)
                            exists = File.Exists(file.Path);
                        else
                            exists = Directory.Exists(file.Path);
                        if (!exists)
                            deletedfiles.Add(file);
                    }
                    this.RefreshAfterCommand();
                }
            }, true);
        }

        public void DeleteAllSelectedFiles()
        {
            ExecuteTaskAsync(() =>
            {
                VMFile[] files = this.Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.MoveToRecycleBin(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file.IsFile)
                            exists = File.Exists(file.Path);
                        else
                            exists = Directory.Exists(file.Path);
                        if (!exists)
                            deletedfiles.Add(file);
                    }
                    this.RefreshAfterCommand();
                }
            }, true);
        }


        #region constructor

        protected VMFolder(VMFolder parent, string name, string fullPath)
            : base(parent, name)
        {
            FileTotal = null;
            this._Path = fullPath;
        }

        [DesignOnly(true)]
        internal VMFolder(VMFolder parent, string name)
            : base(parent, name, null)
        {
            if (parent == null)
                this._Path = name;
            else
                this._Path = System.IO.Path.Combine(parent.Path, name);
            RefreshCount();
        }

        private readonly string _Path;
        public override string Path => _Path;


        #endregion constructor

        #region properties

        public override bool IsFile
        {
            get
            {
                return false;
            }
        }




        public override string Extension
        {
            get
            {
                return string.Empty;
            }
        }



        private ulong? _FileTotal = 1;
        private ulong? _FolderTotal = null;

        public ulong? FileCount
        {
            get { return (ulong?)(this.Childs?.Count - this.Folders?.Count); }
        }

        public override ulong? FileTotal
        {
            get { return _FileTotal; }
            protected set { SetProperty(ref _FileTotal, value); }
        }

        public override ulong? FolderTotal
        {
            get { return _FolderTotal; }
            protected set { SetProperty(ref _FolderTotal, value); }
        }



        protected virtual void SelectTreeItem(VMFolder folder)
        {
            this.Parent.SelectTreeItem(folder);
        }



        protected void SetInternalIsTreeSelected()
        {
            _Attributes |= FileAttributesEx.TreeSelected;
        }

        public bool IsTreeSelected
        {
            get { return (_Attributes & FileAttributesEx.TreeSelected) == FileAttributesEx.TreeSelected; }
            set
            {
                if (value != IsTreeSelected)
                {
                    if (value)
                    {
                        _Attributes |= FileAttributesEx.TreeSelected;
                        //this.IsExpanded = true;    Maybe
                        if (value && this.Parent != null)
                            this.Parent.IsExpanded = true;

                        this.SelectTreeItem(this);
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.TreeSelected;
                    }

                    this.OnPropertyChanged(nameof(IsTreeSelected));
                    if (value && !this.Root.IsDesign && Application.Current != null)
                    {
                        ExecuteTaskAsync(() =>
                        {
                            this.FillChildList();

                            Parallel.ForEach(this.Childs.ToList(), (T) => T.RefreshOnView());

                            RefreshAfterCommand();
                        }, true);
                    }
                }
            }
        }







        public bool IsExpanded
        {
            get { return (_Attributes & FileAttributesEx.Expanded) == FileAttributesEx.Expanded; }
            set
            {
                if (value != IsExpanded)
                {
                    if (value)
                        _Attributes |= FileAttributesEx.Expanded;
                    else
                        _Attributes &= ~FileAttributesEx.Expanded;

                    if (value && this.Parent != null)
                        this.Parent.IsExpanded = true;

                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }





















        public void RefreshAfterCommand()
        {
            ExecuteTaskAsync(() =>
            {
                FillChildList(true);
                RefreshCount();
                RefreshParents();
            });
        }



        private VMFile selectedItem;
        public VMFile SelectedItem { get => selectedItem; set => SetProperty(ref selectedItem, value); }

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
                this.FileTotal = Sum(this.Childs.Select(T => T.FileTotal));
                this.FolderTotal = Sum(this.Folders.Select(T => T.FolderTotal)) + (ulong)this.Folders.Count;
                this.DiskSize = Sum(this.Childs.Select(T => T.DiskSize));
                this.FileSize = Sum(this.Childs.Select(T => T.FileSize));
            }
        }


        public static ulong Sum(IEnumerable<ulong?> source)
        {
            ulong total = 0;

            foreach (var item in source.Where(T => T.HasValue))
                total = total + item ?? 0;

            return total;
        }


        public void RefreshParents()
        {
            if (this.Parent != null)
            {
                this.Parent.RefreshCount();
                this.Parent.RefreshParents();
            }
        }

        private readonly object _lock = new object();

        public ObservableImmutableCollection<VMFile> Childs { get; } = new ObservableImmutableCollection<VMFile>();

        public ObservableImmutableCollection<VMFolder> Folders { get; } = new ObservableImmutableCollection<VMFolder>();


        public void FillChildList(bool refreshOnNew = false)
        {
            lock (_lock)
            {
                try
                {
                    List<VMFile> tmpChilds = Childs.ToList();
                    List<VMFile> addChilds = new List<VMFile>();
                    VMFile found = null;
                    IEnumerable<LittleFileInfo> files = IOHelper.GetFiles(this.Path);
                    foreach (LittleFileInfo fileInfo in files.OrderByDescending(T => T.IsFolder).ThenBy(T => T.FileName))
                    {
                        found = tmpChilds.FirstOrDefault(T => T.Name == fileInfo.FileName);
                        if (found == null)
                        {
                            if (fileInfo.IsFolder)
                            {
                                found = new VMFolder(this, fileInfo.FileName, fileInfo.FullPath);
                                if (refreshOnNew)
                                    (found as VMFolder).Refresh(new ParallelOptions());
                            }
                            else
                                found = new VMFile(this, fileInfo.FileName);
                            addChilds.Add(found);
                            if (refreshOnNew && this.Parent.IsTreeSelected)
                                this.RefreshOnView();
                        }
                        else
                        {
                            tmpChilds.Remove(found);
                        }
                        found.Refresh(fileInfo);
                    }
                    Folders.DoOperation((l) => l.RemoveRange(tmpChilds.OfType<VMFolder>(), EqualityComparer<VMFolder>.Default));
                    Childs.DoOperation((l) => l.RemoveRange(tmpChilds, EqualityComparer<VMFile>.Default));

                    Folders.DoAddRange((l) => addChilds.OfType<VMFolder>());
                    Childs.DoAddRange((l) => addChilds);
                }
                catch (DirectoryNotFoundException ex)
                {
                    this.LogException(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    this.LogException(ex);
                    this.IsProtected = true;
                }
            }
        }

        public virtual void Refresh(ParallelOptions parallelOptions)
        {
            if (this.Root.IsDesign || (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested))
                return;
            try
            {
                this.FillChildList();
                if (this.Childs != null && this.Childs.Count > 0)
                {
                    if (this.IsTreeSelected)
                    {
                        ExecuteTaskAsync(() =>
                        {
                            Parallel.ForEach(this.Childs.ToList(), parallelOptions, (T) => T.RefreshOnView());
                        }, true);
                    }
                    Parallel.ForEach(this.Folders.ToList(), parallelOptions, (T) => T.Refresh(parallelOptions));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
                this.LogException(ex);
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
