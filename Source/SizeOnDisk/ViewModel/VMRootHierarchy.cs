using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.ViewModel
{
    public class VMRootHierarchy : VMFolder
    {

        void _timer_Tick(object sender, EventArgs e)
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

        private DispatcherTimer _Timer;

        public VMRootHierarchy() : base(null, null, null, 0, Dispatcher.CurrentDispatcher)
        {
            //this.Childs = new ObservableCollection<VMFile>();
            //this.Folders = new ObservableCollection<VMFolder>();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                VMRootFolder newFolder = new VMRootFolder(this, "Root Folder");
                //this.Childs.Add(newFolder);
                this.Folders.Add(newFolder);
            }
            else
            {
                _Timer = new DispatcherTimer(DispatcherPriority.DataBind)
                {
                    Interval = new TimeSpan(0, 0, 1)
                };
                _Timer.Tick += new EventHandler(_timer_Tick);
                _Timer.Start();
            }
        }

        #region function

        public void AddRootFolder(string path)
        {
            VMRootFolder newFolder = new VMRootFolder(this, path, path, this.Dispatcher);
            //this.Childs.Add(newFolder);
            this.Folders.Add(newFolder);
            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException("folder", "Can not remove null item");
            this.SelectedRootFolder = null;
            //this.Childs.Remove(folder);
            this.Folders.Remove(folder);
            folder.Dispose();
        }

        public void StopAsync()
        {
            foreach (VMRootFolder folder in this.Folders)
                folder.StopAsync();
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
            CommandManager.InvalidateRequerySuggested();
            this.OnPropertyChanged(nameof(IsRunning));
        }

        public static readonly RoutedCommandEx OpenFolderCommand = new RoutedCommandEx("openfolder", "loc:ChooseFolder", "pack://application:,,,/SizeOnDisk;component/Icons/openfolderHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Insert, ModifierKeys.None, "loc:Insert"));
        public static readonly RoutedCommandEx RefreshCommand = new RoutedCommandEx("refresh", "loc:PresentationCore:ExceptionStringTable:RefreshText", "pack://application:,,,/SizeOnDisk;component/Icons/Refresh.png", typeof(VMRootHierarchy), new KeyGesture(Key.F5, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:RefreshKeyDisplayString"));
        public static readonly RoutedCommandEx StopCommand = new RoutedCommandEx("stop", "loc:PresentationCore:ExceptionStringTable:StopText", "pack://application:,,,/SizeOnDisk;component/Icons/StopHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Escape, ModifierKeys.None, "loc:PresentationCore:ExceptionStringTable:StopKeyDisplayString"));
        public static readonly RoutedCommandEx CloseCommand = new RoutedCommandEx("close", "loc:PresentationCore:ExceptionStringTable:CloseText", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMRootHierarchy));
        public static readonly RoutedUICommand RefreshAllCommand = new RoutedUICommand("Refresh all", "loc:RefreshAll", typeof(VMFile));

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

        /*public override void AddInputModels(InputBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException("bindingCollection", "bindingCollection is null");
            base.AddInputModels(bindingCollection);
            bindingCollection.Add(new InputBinding(RefreshCommand, RefreshCommand.KeyGesture));
        }*/

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
            this.SelectedRootFolder.StopAsync();
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
