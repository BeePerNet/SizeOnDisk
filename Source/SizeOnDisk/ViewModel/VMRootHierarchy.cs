using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        private readonly DispatcherTimer _Timer;

        public VMRootHierarchy() : base(null, null, null, 0)
        {
            this.IsExpanded = true;
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                VMRootFolder newFolder = new VMRootFolder(this, "Root Folder");
                this.Folders.Add(newFolder);

                newFolder.IsExpanded = true;
                newFolder.IsTreeSelected = true;
            }
            else
            {
                //BindingOperations.EnableCollectionSynchronization(this.Folders, this._listlock = new object());
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
            VMRootFolder newFolder = new VMRootFolder(this, path, path);
            this.Folders.Add(newFolder);

            newFolder.IsExpanded = true;
            this.SelectedRootFolder = newFolder;

            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            if (SelectedRootFolder == folder)
                this.SelectedRootFolder = null;
            this.Folders.Remove(folder);
        }

        public void StopAllAsync()
        {
            Task[] tasks = this.Folders.Select(T => (T as VMRootFolder).Stop()).ToArray();
            if (tasks.Length > 0)
                Task.WaitAll(tasks);
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
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CommandManager.InvalidateRequerySuggested();
            }));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedCommandEx OpenFolderCommand = new RoutedCommandEx("openfolder", "loc:ChooseFolder", "pack://application:,,,/SizeOnDisk;component/Icons/openfolderHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Insert, ModifierKeys.None, "loc:Insert"));
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand RefreshAllCommand = new RoutedUICommand("Refresh all", "RefreshAll", typeof(VMRootHierarchy));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException(nameof(bindingCollection));
            bindingCollection.Add(new CommandBinding(OpenFolderCommand, CallOpenCommand));
            bindingCollection.Add(new CommandBinding(RefreshAllCommand, CallRefreshCommand, CanCallRefreshCommand));
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


        private void CanCallRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.Folders.Count > 0;
        }

        private void CallRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            foreach (VMRootFolder folder in this.Folders)
                folder.RefreshAsync();
        }

        #endregion Commands

    }
}
