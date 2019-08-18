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
            Root.ExecuteTask(() =>
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
            Root.ExecuteTask(() =>
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

        public override bool IsLink => (Attributes & FileAttributesEx.ReparsePoint) == FileAttributesEx.ReparsePoint;

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
                            Root.ExecuteTaskAsync(() =>
                            {
                                RefreshOnView();

                                FillChildList();

                                Parallel.ForEach(Childs, Root.GetParallelOptions(), (T) => T.RefreshOnView());

                                RefreshCount();
                                RefreshParents();

                            }, false, true);
                        }
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.TreeSelected;
                        this.GetOutOfView();

                        Root.ExecuteTaskAsync(() =>
                        {
                            foreach(VMFile f in Childs)
                            {
                                f.GetOutOfView();
                            }
                        }, false, false);
                    }

                    OnPropertyChanged(nameof(IsTreeSelected));
                }
            }
        }



        public override string LinkPath
        {
            get
            {
                string result = string.Empty;
                if (this.IsLink)
                {
                    Root.ExecuteTask(() =>
                    {
                        result = new ReparsePoint(Path).Target;
                    }, false);
                }
                return result;
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
            Root.ExecuteTaskAsync(() =>
            {
                FillChildList(true);
                RefreshCount();
                RefreshParents();
            }, false, false);
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
                FolderTotal = Sum(Folders.Select(T => T.FolderTotal)) + (ulong)Folders.Count;
                DiskSize = Sum(Childs.Select(T => T.DiskSize));
                FileSize = Sum(Childs.Select(T => T.FileSize));
            }
        }


        public static ulong Sum(IEnumerable<ulong?> source)
        {
            ulong total = 0;

            foreach (ulong item in source.Where(T => T.HasValue))
            {
                total += item;
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
            if (!this.IsLink)
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
                        Root.ExecuteTaskAsync(() =>
                        {
                            Parallel.ForEach(Childs, parallelOptions, (T) => T.RefreshOnView());
                        }, false, true);
                    }
                    Parallel.ForEach(Folders, parallelOptions, (T) => T.Refresh(parallelOptions));
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

        #endregion functions
    }
}
