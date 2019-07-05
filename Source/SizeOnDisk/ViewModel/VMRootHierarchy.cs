using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand;


namespace SizeOnDisk.ViewModel
{
    public class VMRootHierarchy : VMFolder
    {
        VMRootFolder _SelectedRootFolder;

        public VMRootFolder SelectedRootFolder
        {
            get { return _SelectedRootFolder; }
            set
            {
                if (_SelectedRootFolder != value)
                {
                    _SelectedRootFolder = value;
                    this.OnPropertyChanged("SelectedRootFolder");
                }
            }
        }

        public VMRootHierarchy() : base(null, string.Empty, string.Empty, Dispatcher.CurrentDispatcher)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                VMRootFolder newFolder = new VMRootFolder(this, "Root Folder");
                this.Childs.Add(newFolder);
                this.Folders = new Collection<VMFolder>(this.Childs.OfType<VMFolder>().ToList());
            }
        }

        #region function

        public void AddRootFolder(string path)
        {
            VMRootFolder newFolder = new VMRootFolder(this, path, path, this.Dispatcher);
            this.Childs.Add(newFolder);
            this.Folders = new Collection<VMFolder>(this.Childs.OfType<VMFolder>().ToList());
            this.OnPropertyChanged("Folders");
            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException("folder", "Can not remove null item");
            this.SelectedRootFolder = null;
            this.Childs.Remove(folder);
            this.Folders = new Collection<VMFolder>(this.Childs.OfType<VMFolder>().ToList());
            this.OnPropertyChanged("Folders");
            folder.Dispose();
        }

        public void StopAsync()
        {
            foreach (VMRootFolder folder in this.Childs)
                folder.StopAsync();
        }

        #endregion function

        #region Commands

        public bool IsRunning
        {
            get
            {
                return this.Childs.Cast<VMRootFolder>().Any(T => T.ExecutionState == TaskExecutionState.Running);
            }
        }

        internal void RefreshIsRunning()
        {
            this.OnPropertyChanged("IsRunning");
        }

        public static readonly CommandEx OpenFolderCommand = new CommandEx("openfolder", "ChooseFolder", "pack://application:,,,/SizeOnDisk;component/Icons/openfolderHS.png", typeof(VMRootHierarchy), new KeyGesture(Key.Insert, ModifierKeys.None, "Insert")) { UseDisablingImage = false };
        public static readonly CommandEx RefreshCommand = new CommandEx("refresh", "PresentationCore:ExceptionStringTable:RefreshText", "pack://application:,,,/SizeOnDisk;component/Icons/Refresh.png", typeof(VMRootHierarchy), new KeyGesture(Key.F5, ModifierKeys.None, "F5")) { UseDisablingImage = false };
        public static readonly RoutedCommand RefreshAllCommand = new RoutedCommand("RefreshAll", typeof(VMFile));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException("bindingCollection", "bindingCollection is null");
            base.AddCommandModels(bindingCollection);
            bindingCollection.Add(new CommandBinding(OpenFolderCommand, CallOpenCommand));
            bindingCollection.Add(new CommandBinding(NavigationCommands.Refresh, CallRefreshCommand, CanCallRefreshCommand));
            bindingCollection.Add(new CommandBinding(RefreshAllCommand, CallRefreshCommand, CanCallCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Stop, CallStopCommand, CanCallStopCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Close, CallCloseCommand, CanCallCommand));
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
