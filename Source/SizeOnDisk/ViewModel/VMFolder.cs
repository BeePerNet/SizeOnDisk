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
                string[] filenames = Childs.Where(T => T.IsSelected).Select(T => T.Path).ToArray();

                if (ShellHelper.SafeNativeMethods.PermanentDelete(filenames))
                {
                    RefreshAfterCommand();
                }
            }, true);
        }

        public void DeleteAllSelectedFiles()
        {
            ExecuteTaskAsync(() =>
            {
                string[] filenames = Childs.Where(T => T.IsSelected).Select(T => T.Path).ToArray();

                if (ShellHelper.SafeNativeMethods.MoveToRecycleBin(filenames))
                {
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



        private long? _FileTotal = 1;
        private long? _FolderTotal = null;

        public long? FileCount => (long?)(Childs?.Count - Folders?.Count);

        public override long? FileTotal
        {
            get => _FileTotal;
            protected set => SetProperty(ref _FileTotal, value);
        }

        public override long? FolderTotal
        {
            get => _FolderTotal;
            protected set => SetProperty(ref _FolderTotal, value);
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

                        if (value && Parent != null)
                        {
                            Parent.IsExpanded = true;
                        }

                        Root.SelectedTreeItem = this;
                        Root.SelectedListItem = this;

                        if (value && !Root.IsDesign && Application.Current != null)
                        {
                            this.RefreshOnView();

                            ExecuteTaskAsync(() =>
                            {
                                FillChildList();

                                Parallel.ForEach(Childs.ToList(), Root.GetParallelOptions(), (T) => T.RefreshOnView());

                                RefreshAfterCommand();
                            }, true);
                        }
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.TreeSelected;
                        this.GetOutOfView();

                        ExecuteTaskAsync(() =>
                        {
                            Parallel.ForEach(Childs.ToList(), Root.GetParallelOptions(), (T) => T.GetOutOfView());
                        });
                    }

                    OnPropertyChanged(nameof(IsTreeSelected));
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


        #endregion properties

        #region functions

        public virtual VMFile FindVMFile(string path)
        {
            if (path.StartsWith("\\", StringComparison.Ordinal))
            {
                string subpath = path.Remove(0, 1);
                string fileName = subpath;
                if (fileName.Contains("\\"))
                {
                    int idx = fileName.IndexOf("\\");
                    fileName = fileName.Remove(idx);
                    subpath = subpath.Remove(0, idx);
                }
                else
                {
                    subpath = null;
                }
                VMFile vmfile = Childs.SingleOrDefault(T => T.Name.Equals(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (vmfile == null)
                    throw new FileNotFoundException(null, path);
                if (subpath != null && vmfile is VMFolder folder)
                {
                    return folder.FindVMFile(subpath);
                }
                return vmfile;
            }
            else
            {
                return this.Root.FindVMFile(path);
            }
        }




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
                FolderTotal = Sum(Folders.Select(T => T.FolderTotal)) + (long)Folders.Count;
                DiskSize = Sum(Childs.Select(T => T.DiskSize));
                FileSize = Sum(Childs.Select(T => T.FileSize));
            }
        }


        public static long Sum(IEnumerable<long?> source)
        {
            long total = 0;

            foreach (long? item in source.Where(T => T.HasValue))
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
                    IEnumerable<LittleFileInfo> files = ShellHelper.GetFiles(Path);
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
                    if (tmpChilds.Contains(Root.SelectedTreeItem))
                        Root.SelectedTreeItem = null;
                    if (tmpChilds.Contains(Root.SelectedListItem))
                        Root.SelectedListItem = null;
                    if (tmpChilds.Contains(Root.SelectedItem))
                        Root.SelectedItem = null;

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
