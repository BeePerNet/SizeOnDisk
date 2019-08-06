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
                VMFile[] files = Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.PermanentDelete(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file.IsFile)
                        {
                            exists = File.Exists(file.Path);
                        }
                        else
                        {
                            exists = Directory.Exists(file.Path);
                        }

                        if (!exists)
                        {
                            deletedfiles.Add(file);
                        }
                    }
                    RefreshAfterCommand();
                }
            }, true);
        }

        public void DeleteAllSelectedFiles()
        {
            ExecuteTaskAsync(() =>
            {
                VMFile[] files = Childs.Where(T => T.IsSelected).ToArray();
                string[] filenames = files.Select(T => T.Path).ToArray();

                if (Shell.IOHelper.SafeNativeMethods.MoveToRecycleBin(filenames))
                {
                    List<VMFile> deletedfiles = new List<VMFile>();
                    foreach (VMFile file in files)
                    {
                        bool exists = false;
                        if (file.IsFile)
                        {
                            exists = File.Exists(file.Path);
                        }
                        else
                        {
                            exists = Directory.Exists(file.Path);
                        }

                        if (!exists)
                        {
                            deletedfiles.Add(file);
                        }
                    }
                    RefreshAfterCommand();
                }
            }, true);
        }


        #region constructor

        protected VMFolder(VMFolder parent, string name, string fullPath)
            : base(parent, name)
        {
            FileTotal = null;
            _Path = fullPath;
        }

        [DesignOnly(true)]
        internal VMFolder(VMFolder parent, string name)
            : base(parent, name, null)
        {
            if (parent == null)
            {
                _Path = name;
            }
            else
            {
                _Path = System.IO.Path.Combine(parent.Path, name);
            }

            RefreshCount();
        }

        private readonly string _Path;
        public override string Path => _Path;


        #endregion constructor

        #region properties

        public override bool IsFile => false;




        public override string Extension => string.Empty;



        private ulong? _FileTotal = 1;
        private ulong? _FolderTotal = null;

        public ulong? FileCount => (ulong?)(Childs?.Count - Folders?.Count);

        public override ulong? FileTotal
        {
            get => _FileTotal;
            protected set => SetProperty(ref _FileTotal, value);
        }

        public override ulong? FolderTotal
        {
            get => _FolderTotal;
            protected set => SetProperty(ref _FolderTotal, value);
        }



        protected virtual void SelectTreeItem(VMFolder folder)
        {
            Parent.SelectTreeItem(folder);
        }



        protected void SetInternalIsTreeSelected()
        {
            _Attributes |= FileAttributesEx.TreeSelected;
        }

        public bool IsTreeSelected
        {
            get => (_Attributes & FileAttributesEx.TreeSelected) == FileAttributesEx.TreeSelected;
            set
            {
                if (value != IsTreeSelected)
                {
                    if (value)
                    {
                        _Attributes |= FileAttributesEx.TreeSelected;
                        //this.IsExpanded = true;    Maybe
                        if (value && Parent != null)
                        {
                            Parent.IsExpanded = true;
                        }

                        SelectTreeItem(this);
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.TreeSelected;
                    }

                    OnPropertyChanged(nameof(IsTreeSelected));
                    if (value && !Root.IsDesign && Application.Current != null)
                    {
                        ExecuteTaskAsync(() =>
                        {
                            FillChildList();

                            Parallel.ForEach(Childs.ToList(), (T) => T.RefreshOnView());

                            RefreshAfterCommand();
                        }, true);
                    }
                }
            }
        }







        public bool IsExpanded
        {
            get => (_Attributes & FileAttributesEx.Expanded) == FileAttributesEx.Expanded;
            set
            {
                if (value != IsExpanded)
                {
                    if (value)
                    {
                        _Attributes |= FileAttributesEx.Expanded;
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.Expanded;
                    }

                    if (value && Parent != null)
                    {
                        Parent.IsExpanded = true;
                    }

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
            OnPropertyChanged(nameof(FileCount));
            if (IsProtected)
            {
                FileTotal = null;
                FolderTotal = null;
                DiskSize = null;
                FileSize = null;
            }
            else
            {
                FileTotal = Sum(Childs.Select(T => T.FileTotal));
                FolderTotal = Sum(Folders.Select(T => T.FolderTotal)) + (ulong)Folders.Count;
                DiskSize = Sum(Childs.Select(T => T.DiskSize));
                FileSize = Sum(Childs.Select(T => T.FileSize));
            }
        }


        public static ulong Sum(IEnumerable<ulong?> source)
        {
            ulong total = 0;

            foreach (ulong? item in source.Where(T => T.HasValue))
            {
                total = total + item ?? 0;
            }

            return total;
        }


        public void RefreshParents()
        {
            if (Parent != null)
            {
                Parent.RefreshCount();
                Parent.RefreshParents();
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
                    IEnumerable<LittleFileInfo> files = IOHelper.GetFiles(Path);
                    foreach (LittleFileInfo fileInfo in files.OrderByDescending(T => T.IsFolder).ThenBy(T => T.FileName))
                    {
                        found = tmpChilds.FirstOrDefault(T => T.Name == fileInfo.FileName);
                        if (found == null)
                        {
                            if (fileInfo.IsFolder)
                            {
                                found = new VMFolder(this, fileInfo.FileName, fileInfo.FullPath);
                                if (refreshOnNew)
                                {
                                    (found as VMFolder).Refresh(new ParallelOptions());
                                }
                            }
                            else
                            {
                                found = new VMFile(this, fileInfo.FileName);
                            }

                            addChilds.Add(found);
                            if (refreshOnNew && Parent.IsTreeSelected)
                            {
                                RefreshOnView();
                            }
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
                    LogException(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogException(ex);
                    IsProtected = true;
                }
            }
        }

        public virtual void Refresh(ParallelOptions parallelOptions)
        {
            if (Root.IsDesign || (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested))
            {
                return;
            }

            try
            {
                FillChildList();
                if (Childs != null && Childs.Count > 0)
                {
                    if (IsTreeSelected)
                    {
                        ExecuteTaskAsync(() =>
                        {
                            Parallel.ForEach(Childs.ToList(), parallelOptions, (T) => T.RefreshOnView());
                        }, true);
                    }
                    Parallel.ForEach(Folders.ToList(), parallelOptions, (T) => T.Refresh(parallelOptions));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
                LogException(ex);
            }
            if (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            RefreshCount();
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
