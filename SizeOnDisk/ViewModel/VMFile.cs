using GongSolutions.Wpf.DragDrop;
using SizeOnDisk.Shell;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.ViewModel
{
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
    [DebuggerDisplay("{GetType().Name}: {Name}")]
    public class VMFile : CommandViewModel, IDragSource, IDropTarget
    {
        private const string MessageIsNotVMFile = "OriginalSource is not VMFile";


        public static readonly RoutedCommandEx OpenCommand = new RoutedCommandEx("open", "loc:PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        public static readonly RoutedCommandEx EditCommand = new RoutedCommandEx("edit", "loc:Edit", typeof(VMFile), new KeyGesture(Key.E, ModifierKeys.Control, "loc:EditKey"));
        public static readonly RoutedCommandEx OpenAsCommand = new RoutedCommandEx("openas", "loc:OpenAs", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "loc:OpenAsKey"));
        public static readonly RoutedCommandEx PrintCommand = new RoutedCommandEx("print", "loc:PresentationCore:ExceptionStringTable:PrintText", "pack://application:,,,/SizeOnDisk;component/Icons/PrintHS.png", typeof(VMFile), new KeyGesture(Key.P, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:PrintKeyDisplayString"));
        public static readonly RoutedCommandEx ShellSelectCommand = new RoutedCommandEx("shellselect", "loc:Explore", "pack://application:,,,/SizeOnDisk;component/Icons/Explore.png", typeof(VMFile), new KeyGesture(Key.N, ModifierKeys.Control, "ExploreKey"));
        public static readonly RoutedCommandEx FindCommand = new RoutedCommandEx("find", "loc:PresentationCore:ExceptionStringTable:FindText", "pack://application:,,,/SizeOnDisk;component/Icons/SearchFolderHS.png", typeof(VMFile), new KeyGesture(Key.F, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:FindKeyDisplayString"));
        public static readonly RoutedCommandEx DeleteCommand = new RoutedCommandEx("delete", "loc:PresentationCore:ExceptionStringTable:DeleteText", "pack://application:,,,/SizeOnDisk;component/Icons/Recycle_Bin.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:DeleteKeyDisplayString"));
        public static readonly RoutedCommandEx PermanentDeleteCommand = new RoutedCommandEx("permanentdelete", "loc:PermanentDelete", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.Shift, "loc:PermanentDeleteKey"));
        public static readonly RoutedCommandEx PropertiesCommand = new RoutedCommandEx("properties", "loc:PresentationCore:ExceptionStringTable:PropertiesText", "pack://application:,,,/SizeOnDisk;component/Icons/Properties.png", typeof(VMFile), new KeyGesture(Key.F4, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:PropertiesKeyDisplayString"));
        public static readonly RoutedCommandEx FollowLinkCommand = new RoutedCommandEx("followlink", "loc:FollowLink", "pack://application:,,,/SizeOnDisk;component/Icons/Shortcut.png", typeof(VMFile), new KeyGesture(Key.Enter, ModifierKeys.Alt));
        public static readonly RoutedCommandEx SelectCommand = new RoutedCommandEx("select", "Select", "pack://application:,,,/SizeOnDisk;component/Icons/Select.png", typeof(VMFile));
        public static readonly RoutedCommandEx CopyCommand = new RoutedCommandEx("copy", "loc:PresentationCore:ExceptionStringTable:CopyText", typeof(VMFile), new KeyGesture(Key.C, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:CopyKeyDisplayString"));
        public static readonly RoutedCommandEx CutCommand = new RoutedCommandEx("cut", "loc:PresentationCore:ExceptionStringTable:CutText", typeof(VMFile), new KeyGesture(Key.X, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:CutKeyDisplayString"));
        public static readonly RoutedCommandEx PasteCommand = new RoutedCommandEx("paste", "loc:PresentationCore:ExceptionStringTable:PasteText", typeof(VMFile), new KeyGesture(Key.V, ModifierKeys.Control, "loc:PresentationCore:ExceptionStringTable:PasteKeyDisplayString"));


        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
            {
                return;
            }

            bindingCollection.Add(new CommandBinding(OpenCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(OpenAsCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(EditCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(PrintCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ShellSelectCommand, CallShellSelectCommand, CanCallCommand));
            //TODO: bindingCollection.Add(new CommandBinding(FindCommand, CallShellCommand, CanCallCommand));
            bindingCollection.Add(new CommandBinding(DeleteCommand, CallDeleteCommand, CanCallSelectedItemsCommand));
            bindingCollection.Add(new CommandBinding(PermanentDeleteCommand, CallPermanentDeleteCommand, CanCallSelectedItemsCommand));
            bindingCollection.Add(new CommandBinding(PropertiesCommand, CallShellCommand));
            bindingCollection.Add(new CommandBinding(FollowLinkCommand, CallFollowLinkCommand, CanCallFollowLinkCommand));
            bindingCollection.Add(new CommandBinding(SelectCommand, CallSelectCommand));
            bindingCollection.Add(new CommandBinding(CopyCommand, CallCutCopyCommand, CanCallSelectedItemsCommand));
            bindingCollection.Add(new CommandBinding(CutCommand, CallCutCopyCommand, CanCallSelectedItemsCommand));
            bindingCollection.Add(new CommandBinding(PasteCommand, CallPasteCommand, CanCallPasteCommand));
        }


        public override void AddInputModels(InputBindingCollection bindingCollection)
        {
        }

        #region constructor

        internal VMFile(VMFolder parent, string name)
        {
            _Name = name;
            Parent = parent;
        }


        [DesignOnly(true)]
        internal VMFile(VMFolder parent, string name, ulong? fileSize) : this(parent, name)
        {
            if (fileSize.HasValue)
            {
                FileSize = fileSize;
                DiskSize = Convert.ToUInt64(Math.Ceiling((double)fileSize / 4096) * 4096);
            }
            if (parent != null)
            {
                Details = new VMFileDetails(this, DateTime.Now.AddHours(parent.Childs.Count));
            }
        }


        #endregion constructor

        #region properties

        [Display(Name = "Path")]
        public virtual string Path { get => System.IO.Path.Combine(Parent.Path, Name); }

        private string _Name;
        [Display(ResourceType = typeof(Languages.Localization), Name = nameof(Languages.Localization.Name))]
        [Required]
        [CustomValidation(typeof(VMFile), nameof(ValidateName))]
        [StringLength(260)]
        [MinLength(1)]
        public string Name { get => _Name; set => Rename(value); }

        public virtual VMRootFolder Root => Parent.Root;

        [Browsable(false)]
        [Display(AutoGenerateField = false)]
        public VMFolder Parent { get; }

        [SuppressMessage("Design", "CA2213")]
        private VMFileDetails _Details;
        public VMFileDetails Details
        {
            get
            {
                if (_Details == null)
                {
                    _Details = new VMFileDetails(this);
                }
                return _Details;
            }
            private set => SetProperty(ref _Details, value);
        }


        public virtual string Extension => System.IO.Path.GetExtension(Name).Replace(".", "");

        protected FileAttributesEx _Attributes = FileAttributesEx.Normal;
        [Display(ResourceType = typeof(Languages.Localization), Name = nameof(Languages.Localization.Attributes))]
        [EnumDataType(typeof(FileAttributesEx))]
        public FileAttributesEx Attributes => _Attributes;

        public virtual bool IsFile => true;

        private ulong _FileSize = 0;
        private ulong _DiskSize = 0;

        public virtual ulong? FileTotal { get => 1; protected set { } }
        public virtual ulong? FolderTotal { get => null; protected set { } }
        public ulong? DiskSize
        {
            get
            {
                if ((_Attributes & FileAttributesEx.DiskSizeValue) == FileAttributesEx.DiskSizeValue)
                    return _DiskSize;
                return null;
            }
            protected set
            {
                if (value != DiskSize)
                {
                    if (value.HasValue)
                    {
                        _DiskSize = value.Value;
                        _Attributes |= FileAttributesEx.DiskSizeValue;
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.DiskSizeValue;
                    }
                    OnPropertyChanged(nameof(DiskSize));
                }
            }
        }
        public ulong? FileSize
        {
            get
            {
                if ((_Attributes & FileAttributesEx.FileSizeValue) == FileAttributesEx.FileSizeValue)
                    return _FileSize;
                return null;
            }
            protected set
            {
                if (value != FileSize)
                {
                    if (value.HasValue)
                    {
                        _FileSize = value.Value;
                        _Attributes |= FileAttributesEx.FileSizeValue;
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.FileSizeValue;
                    }
                    OnPropertyChanged(nameof(FileSize));
                }
            }
        }

        public bool IsProtected
        {
            get => (_Attributes & FileAttributesEx.Protected) == FileAttributesEx.Protected;
            protected set
            {
                if (value != IsProtected)
                {
                    if (value)
                    {
                        _Attributes |= FileAttributesEx.Protected;
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.Protected;
                    }

                    OnPropertyChanged(nameof(IsProtected));
                }
            }
        }


        public bool IsSelected
        {
            get => (_Attributes & FileAttributesEx.Selected) == FileAttributesEx.Selected;
            set
            {
                if (value != IsSelected)
                {
                    if (value)
                    {
                        _Attributes |= FileAttributesEx.Selected;
                    }
                    else
                    {
                        _Attributes &= ~FileAttributesEx.Selected;
                    }

                    if (value)
                    {
                        Root.SelectedListItem = this;
                    }
                    else if (Root.SelectedListItem == this)
                    {
                        Root.SelectedListItem = this.Parent.Childs.FirstOrDefault(T => T.IsSelected) ?? this;
                    }
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        #endregion properties

        #region functions

        public void LogException(Exception ex)
        {
            TextExceptionFormatter formatter = new TextExceptionFormatter(ex);
            Root.Log(new VMLog(this, formatter.GetInnerException().Message, formatter.Format()));
        }


        public static void ValidateName(string name)
        {
            int i = name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars());
            if (i != -1)
            {
                throw new ArgumentException("Invalid character: " + name[i], nameof(name));
            }
        }

        public void Rename(string newName)
        {
            if (System.IO.Path.GetFileName(Path) != newName)
            {
                Root.ExecuteTaskAsync(() =>
                {
                    ValidateName(newName);

                    string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), newName);
                    if (IsFile)
                    {
                        File.Move(Path, newPath);
                    }
                    else
                    {
                        Directory.Move(Path, newPath);
                    }
                    _Name = newName;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Path));
                    OnPropertyChanged(nameof(Extension));
                    RefreshOnView();
                    Parent.RefreshAfterCommand();
                }, true, true);
            }
        }



        public virtual bool IsLink => Extension.ToUpperInvariant() == "LNK";

        internal virtual void Refresh(LittleFileInfo fileInfo)
        {
            _Attributes = ((FileAttributesEx)fileInfo.Attributes) | (_Attributes & FileAttributesEx.Mask);
            OnPropertyChanged(nameof(Attributes));
            OnPropertyChanged(nameof(IsLink));
            if (IsFile)
            {
                FileSize = fileInfo.Size;
                DiskSize = (((fileInfo.CompressedSize ?? FileSize) + Root.ClusterSize - 1) / Root.ClusterSize) * Root.ClusterSize;
            }
        }

        public void RefreshOnView()
        {
            LittleFileInfo fileInfo = Details.Load();
            Refresh(fileInfo);
        }

        public void GetOutOfView()
        {
            Details = null;
            IsSelected = false;
        }

        #endregion functions

        #region Commands


        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallSelectCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            object viewModel = GetViewModelObject(e.OriginalSource);

            VMFile file = viewModel as VMFile;
            if (file == null && viewModel is VMLog vmLog)
                file = vmLog.File;

            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            file.Select();
        }

        private void Select()
        {
            Parent.Childs.AsParallel().ForAll(T => T.IsSelected = false);
            Root.Parent.SelectedRootFolder = Root;
            Parent.IsTreeSelected = true;
            Root.SelectedItem = this;
        }

        private void CanCallFollowLinkCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file != null)
            {
                e.Handled = true;
                e.CanExecute = !file.IsProtected && file.IsLink;
            }
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallFollowLinkCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            string path = file.LinkPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new FileFormatException($"The file {file.Path} do not contains destination path.");
            }

            VMFile vmfile = file.Parent.FindVMFile(path);
            if (vmfile == null)
            {
                ShellHelper.ShellExecuteSelect(path);
            }
            else
            {
                vmfile.Select();
            }
        }

        public virtual string LinkPath
        {
            get
            {
                string result = string.Empty;
                if (this.IsLink)
                {
                    Root.ExecuteTask(() =>
                    {
                        result = ShellHelper.GetShellLinkPath(Path);
                    }, false);
                }
                return result;
            }
        }



        private void CanCallSelectedItemsCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file != null)
            {
                e.Handled = true;
                e.CanExecute = file.GetSelectedFiles().All(T => !T.IsProtected && !(T is VMRootFolder));
            }
        }

        public IEnumerable<VMFile> GetSelectedFiles()
        {
            if (!this.IsSelected)
            {
                return new VMFile[] { this };
            }
            else
            {
                return this.Parent.GetSelectedFiles();
            }
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            file.Root.ExecuteTaskAsync(() =>
            {
                if (ShellHelper.MoveToRecycleBin(file.GetSelectedFiles().Select(T => T.Path).ToArray()))
                {
                    file.Parent.RefreshAfterCommand();
                }
            }, true, true);
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallPermanentDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            file.Root.ExecuteTaskAsync(() =>
            {
                if (ShellHelper.PermanentDelete(file.GetSelectedFiles().Select(T => T.Path).ToArray()))
                {
                    file.Parent.RefreshAfterCommand();
                }
            }, true, true);
        }


        private void CanCallCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            if (GetViewModelObject<VMFile>(e.OriginalSource) != null)
            {
                e.Handled = true;
                e.CanExecute = true;
            }
        }

        private void CallShellSelectCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentOutOfRangeException(nameof(e), MessageIsNotVMFile);
            }

            ShellHelper.ShellExecuteSelect(file.Path);
        }


        private void CanCallShellCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                return;
            }

            e.Handled = true;
            e.CanExecute = false;

            if (!(e.Command is RoutedCommand command))
            {
                return;
            }

            if (file.IsProtected)
            {
                e.CanExecute = false;
                return;
            }
            if (command == OpenAsCommand)
            {
                e.CanExecute = true;
                return;
            }
            if (command == OpenCommand && !file.IsFile)
            {
                e.CanExecute = true;
                return;
            }
            if (command == OpenCommand && string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(file.Path)))
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = file.Details != null && file.Details.Verbs != null && file.Details.Verbs.Any(T => T == command.Name);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentOutOfRangeException(nameof(e), MessageIsNotVMFile);
            }

            if (!(e.Command is RoutedCommand command))
            {
                throw new ArgumentOutOfRangeException(nameof(e), "Command is not RoutedCommand");
            }

            if ((e.Command as IMenuCommand)?.Tag != null)
            {
                file.ExecuteCommand(command as IMenuCommand, e);
                return;
            }

            file.Root.ExecuteTaskAsyncByDispatcher(() =>
            {
                if (command == OpenAsCommand)
                {
                    ShellHelper.ShellExecute("Rundll32.exe", $"Shell32.dll,OpenAs_RunDLL {file.Path}");
                    return;
                }
                string path = file.Path;
                if (e.Command == FindCommand && (file.IsFile || file.IsProtected))
                {
                    path = file.Parent.Path;
                }

                ShellHelper.ShellExecute(path, null, command.Name.ToLowerInvariant());
            }, true, Dispatcher.CurrentDispatcher);
        }

        #endregion Commands

        internal bool CanExecuteCommand(IMenuCommand command, object _)
        {
            return !IsProtected && !string.IsNullOrEmpty(command.Tag);
        }

        internal void ExecuteCommand(IMenuCommand command, object _)
        {
            this.Root.ExecuteTaskAsyncByDispatcher(() =>
            {
                string cmd = command.Tag;
                if (cmd.StartsWith("Id:", StringComparison.Ordinal))
                {
                    cmd = cmd.Substring(3);
                    ShellHelper.Activate(cmd, Path, command.Name);
                }
                else if (cmd.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
                {
                    cmd = cmd.Substring(4);
                    ShellHelper.ShellExecute(cmd, $"\"{Path}\"");
                }
                else if (cmd.StartsWith("dll:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotImplementedException($"Name:{command.Name}, Text:{command.Text}, Command{command.Tag}");
                }
                else
                {
                    Tuple<string, string> cmdParam = ShellHelper.SplitCommandAndParameters(cmd);
                    string parameters = cmdParam.Item2;

                    string workingDirectory = Path;
                    if (IsFile)
                    {
                        workingDirectory = Parent.Path;
                    }

                    if (parameters.Contains('%'))
                    {
                        parameters = Regex.Replace(parameters, "%1", Path, RegexOptions.IgnoreCase);
                        parameters = Regex.Replace(parameters, "%l", Path, RegexOptions.IgnoreCase);
                        parameters = Regex.Replace(parameters, "%v", workingDirectory, RegexOptions.IgnoreCase);
                        parameters = Regex.Replace(parameters, "%w", workingDirectory, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        parameters = string.Concat(parameters, "\"", Path, "\"");
                    }

                    ShellHelper.ShellExecute(cmdParam.Item1, parameters);
                }
            }, true, Dispatcher.CurrentDispatcher);
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            //dragInfo.DataObject = new DataObject(DataFormats.FileDrop, dragInfo.SourceItems.Cast<VMFile>().Select(T => T.Path).ToArray());
            dragInfo.DataObject = GetCopyDataObject(dragInfo.SourceItems.Cast<VMFile>());

            //dragInfo.DataFormat = DataFormats.GetDataFormat(DataFormats.FileDrop);
            //dragInfo.Data = dragInfo.SourceItems.Cast<VMFile>().Select(T => T.Path).ToArray();
            dragInfo.Effects = DragDropEffects.All;
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.DataFormat == GongSolutions.Wpf.DragDrop.DragDrop.DataFormat)
                return dragInfo.SourceItem != null && dragInfo.SourceItems.Cast<VMFile>().All(T => !T.IsProtected && !(T is VMRootFolder));
            else
                return false;
        }

        public void Dropped(IDropInfo dropInfo)
        {
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            (dragInfo.SourceItem as VMFile).Parent.RefreshAfterCommand();
        }

        public void DragCancelled()
        {
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            ExceptionBox.ShowException(exception);
            this.Root.LogException(exception);
            return true;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            Tuple<VMFolder, string[]> infos = GetDropInfoValues(dropInfo);
            if (infos == null)
                return;
            if (infos.Item2 != null && infos.Item2.Length > 0)
            {
                string first = infos.Item2.First();
                if (infos.Item1.Path == System.IO.Path.GetDirectoryName(first))
                    return;
                if (Keyboard.Modifiers == ModifierKeys.Control)
                    dropInfo.Effects = DragDropEffects.Copy;
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    dropInfo.Effects = DragDropEffects.Move;
                else
                    dropInfo.Effects = System.IO.Path.GetPathRoot(infos.Item1.Path) == System.IO.Path.GetPathRoot(first) ? DragDropEffects.Move : DragDropEffects.Copy;
                dropInfo.DropTargetAdorner = dropInfo.TargetItem == null ? DropTargetAdorners.Insert : DropTargetAdorners.Highlight;
            }
        }

        private static Tuple<VMFolder, string[]> GetDropInfoValues(IDropInfo dropInfo)
        {
            VMFolder targetItem = dropInfo.TargetItem as VMFolder;
            if (targetItem == null && !(dropInfo.TargetItem is VMFile))
                targetItem = (dropInfo.VisualTarget as FrameworkElement)?.DataContext as VMFolder;
            if (targetItem == null || targetItem.IsProtected)
                return null;
            if (dropInfo.Data is IDataObject dataObject)
            {
                if (dataObject.GetDataPresent(DataFormats.FileDrop))
                {
                    return new Tuple<VMFolder, string[]>(targetItem, dataObject.GetData(DataFormats.FileDrop) as string[]);
                }
            }
            return null;
        }

        private void CanCallPasteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file != null)
            {
                e.Handled = true;
                e.CanExecute = file is VMFolder && !(file.GetSelectedFiles().Count() > 1) && !file.IsProtected && Clipboard.ContainsFileDropList();
            }
        }

        private void CallPasteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFolder file = GetViewModelObject<VMFolder>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            file.Root.ExecuteTask(() =>
            {
                if (Clipboard.GetData("Preferred DropEffect") is MemoryStream obj)
                {
                    StringCollection files = Clipboard.GetFileDropList();
                    bool copy = true;
                    if (obj != null)
                    {
                        byte[] values = obj.ToArray();
                        copy = ((DragDropEffects)values[0] & DragDropEffects.Copy) == DragDropEffects.Copy;
                    }

                    file.DoPaste(copy, files.Cast<string>().ToArray());
                }
            }, true);
        }

        public void Drop(IDropInfo dropInfo)
        {
            Tuple<VMFolder, string[]> infos = GetDropInfoValues(dropInfo);
            if (infos == null)
                return;

            infos.Item1.DoPaste((dropInfo.Effects & DragDropEffects.Copy) == DragDropEffects.Copy, infos.Item2);
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void CallCutCopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(e), MessageIsNotVMFile);
            }

            file.Root.ExecuteTask(() =>
            {
                IEnumerable<VMFile> list = file.GetSelectedFiles();
                IDataObject obj = GetCopyDataObject(list, e.Command == CutCommand);
                Clipboard.Clear();
                Clipboard.SetDataObject(obj, false);
                if (e.Command == CutCommand)
                {
                    VMFile cutfile = list.FirstOrDefault();
                    Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                    file.Root.ExecuteTaskAsync(() =>
                    {
                        bool loop = cutfile != null;
                        while (loop)
                        {
                            if (cutfile is VMFolder)
                            {
                                if (!Directory.Exists(cutfile.Path))
                                {
                                    loop = false;
                                }
                            }
                            else
                            {
                                if (!File.Exists(cutfile.Path))
                                {
                                    loop = false;
                                }
                            }
                            if (loop)
                            {
                                dispatcher.Invoke(() =>
                                {
                                    if (!Clipboard.IsCurrent(obj))
                                        loop = false;
                                });
                            }
                            if (loop)
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        file.Parent.RefreshAfterCommand();
                    }, true, false);
                }
            }, true);
        }

        private static IDataObject GetCopyDataObject(IEnumerable<VMFile> files, bool? cut = null)
        {
            IDataObject data = new DataObject(DataFormats.FileDrop, files.Select(T => T.Path).ToArray());
            if (cut.HasValue)
            {
                using (MemoryStream memo = new MemoryStream(4))
                {
                    byte[] bytes = new byte[] { (byte)(cut.Value ? 2 : 5), 0, 0, 0 };
                    memo.Write(bytes, 0, bytes.Length);
                    data.SetData("Preferred DropEffect", memo);
                }
            }
            return data;
        }

    }
}
