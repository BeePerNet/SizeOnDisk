using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.ViewModel
{
    public class VMRootHierarchy : VMFolder
    {

        void TimerTick(object sender, EventArgs e)
        {
            RunningThreads = Process.GetCurrentProcess().Threads.Count;
        }



        private int _RunningThreads = 0;
        public int RunningThreads
        {
            get { return _RunningThreads; }
            set { SetProperty(ref _RunningThreads, value); }
        }

        VMRootFolder _SelectedRootFolder;

        public VMRootFolder SelectedRootFolder
        {
            get { return _SelectedRootFolder; }
            set { SetProperty(ref _SelectedRootFolder, value); }
        }

        VMFolder _SelectedTreeItem;

        public VMFolder SelectedTreeItem
        {
            get { return _SelectedTreeItem; }
            set { SetProperty(ref _SelectedTreeItem, value); }
        }

        VMFile _SelectedListItem;

        public VMFile SelectedListItem
        {
            get { return _SelectedListItem; }
            set { SetProperty(ref _SelectedListItem, value); }
        }

        private readonly DispatcherTimer _Timer;

        public VMRootHierarchy() : base(null, null, null, 0, Dispatcher.CurrentDispatcher)
        {
            this.IsExpanded = true;
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                VMRootFolder newFolder = new VMRootFolder(this, "Root Folder", "\\\\Root Folder");
                this.Folders.Add(newFolder);
                newFolder.IsExpanded = true;
                newFolder.IsTreeSelected = true;
                newFolder.Childs.Last().IsSelected = true;
            }
            else
            {
                BindingOperations.EnableCollectionSynchronization(this.Childs, _myCollectionLock);
                BindingOperations.EnableCollectionSynchronization(this.Folders, _myCollectionLock);
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind)
                {
                    Interval = new TimeSpan(0, 0, 1)
                };
                _Timer.Tick += new EventHandler(TimerTick);
                _Timer.Start();
            }
        }

        #region function

        public void AddRootFolder(string path)
        {
            VMRootFolder newFolder = new VMRootFolder(this, path, path, this.Dispatcher);
            //No need on root: this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);

            this.SelectedRootFolder = newFolder;
            this.SelectedTreeItem = newFolder;
            this.SelectedListItem = newFolder;
            newFolder.IsExpanded = true;
            newFolder.IsTreeSelected = true;

            BindingOperations.EnableCollectionSynchronization(newFolder.Childs, newFolder._myCollectionLock);
            BindingOperations.EnableCollectionSynchronization(newFolder.Folders, newFolder._myCollectionLock);

            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException("folder", "Can not remove null item");
            this.SelectedRootFolder = null;
            this.Folders.Remove(folder);
            if (this.Folders.Count == 0)
            {
                this.SelectedTreeItem = null;
                this.SelectedListItem = null;
            }
            folder.Dispose();
        }

        public void StopAsync()
        {
            foreach (VMRootFolder folder in this.Folders)
                folder.Stop();
        }

        #endregion function

        #region Commands

        public bool IsRunning
        {
            get
            {
                return this.Folders.Cast<VMRootFolder>().Any(T => T.ExecutionState == TaskExecutionState.Running);
            }
        }

        internal void RefreshIsRunning()
        {
            this.OnPropertyChanged(nameof(IsRunning));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CommandManager.InvalidateRequerySuggested();
            }));
        }

        public static readonly RoutedCommandEx OpenFolderCommand = new RoutedCommandEx("openfolder", "loc:ChooseFolder", "pack://application:,,,/SizeOnDisk;component/Icons/openfolderHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Insert, ModifierKeys.None, "loc:Insert"));
        public static readonly RoutedCommandEx RefreshCommand = new RoutedCommandEx("refresh", "loc:PresentationCore:ExceptionStringTable:RefreshText", "pack://application:,,,/SizeOnDisk;component/Icons/Refresh.png", typeof(VMRootHierarchy), new KeyGesture(Key.F5, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:RefreshKeyDisplayString"));
        public static readonly RoutedCommandEx StopCommand = new RoutedCommandEx("stop", "loc:PresentationCore:ExceptionStringTable:StopText", "pack://application:,,,/SizeOnDisk;component/Icons/StopHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Escape, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:StopKeyDisplayString"));
        public static readonly RoutedCommandEx CloseCommand = new RoutedCommandEx("close", "loc:PresentationCore:ExceptionStringTable:CloseText", "pack://application:,,,/SizeOnDisk;component/Icons/Close.png", typeof(VMRootHierarchy));
        public static readonly RoutedUICommand RefreshAllCommand = new RoutedUICommand("Refresh all", "RefreshAll", typeof(VMFile));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException("bindingCollection", "bindingCollection is null");
            base.AddCommandModels(bindingCollection);
            bindingCollection.Add(new CommandBinding(OpenFolderCommand, CallOpenCommand));
            bindingCollection.Add(new CommandBinding(RefreshCommand, CallRefreshCommand, CanCallRefreshCommand));
            bindingCollection.Add(new CommandBinding(RefreshAllCommand, CallRefreshCommand, CanCallCommand));
            bindingCollection.Add(new CommandBinding(StopCommand, CallStopCommand, CanCallStopCommand));
            bindingCollection.Add(new CommandBinding(CloseCommand, CallCloseCommand, CanCallCommand));
        }

        private void CallOpenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            OpenFolderDialog dialog = new OpenFolderDialog();
            if (dialog.ShowDialog(new WrapperIWin32Window(System.Windows.Application.Current.MainWindow)))
            {
                this.AddRootFolder(dialog.Folder);
            }
        }

        private void CanCallStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.SelectedRootFolder != null && this.SelectedRootFolder.ExecutionState == TaskExecutionState.Running;
        }

        private void CallStopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.SelectedRootFolder.Stop();
        }

        private void CallCloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            CallStopCommand(sender, e);
            this.RemoveRootFolder(this.SelectedRootFolder);
        }

        private void CanCallRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.SelectedRootFolder != null && this.SelectedRootFolder.ExecutionState != TaskExecutionState.Running;
        }

        private void CallRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (e.Command == RefreshAllCommand)
            {
                foreach (VMRootFolder folder in this.Childs)
                    folder.RefreshAsync();
            }
            else
            {
                this.SelectedRootFolder.RefreshAsync();
            }
        }

        private void CanCallCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.SelectedRootFolder != null;
        }

        #endregion Commands

        private VMViewMode viewMode = VMViewMode.Details;
        public VMViewMode ViewMode { get => viewMode; set => SetProperty(ref viewMode, value); }
    }
}
