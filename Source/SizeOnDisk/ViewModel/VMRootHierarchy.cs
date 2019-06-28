using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SizeOnDisk.Utilities;
using System.Windows.Threading;
using System.Windows;
using System.ComponentModel;

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
                this.Folders = new Collection<VMFolder>(this.Childs.Cast<VMFolder>().ToList());
            }
        }

        #region function

        public void AddRootFolder(string path)
        {
            VMRootFolder newFolder = new VMRootFolder(this, path, path, this.Dispatcher);
            this.Childs.Add(newFolder);
            this.Folders = new Collection<VMFolder>(this.Childs.Cast<VMFolder>().ToList());
            this.OnPropertyChanged("Folders");
            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException("folder", "Can not remove null item");
            this.SelectedRootFolder = null;
            this.Childs.Remove(folder);
            this.Folders = new Collection<VMFolder>(this.Childs.Cast<VMFolder>().ToList());
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

        public static readonly RoutedCommand RefreshAllCommand = new RoutedCommand("RefreshAll", typeof(VMFile));

        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            if (bindingCollection == null)
                throw new ArgumentNullException("bindingCollection", "bindingCollection is null");
            base.AddCommandModels(bindingCollection);
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Open, CallOpenCommand));
            bindingCollection.Add(new CommandBinding(NavigationCommands.Refresh, CallRefreshCommand, CanCallRefreshCommand));
            bindingCollection.Add(new CommandBinding(RefreshAllCommand, CallRefreshCommand, CanCallCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Stop, CallStopCommand, CanCallStopCommand));
            bindingCollection.Add(new CommandBinding(ApplicationCommands.Close, CallCloseCommand, CanCallCommand));
        }

        private void CallOpenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            try
            {
                if (dialog.ShowDialog(new WrapperIWin32Window(System.Windows.Application.Current.MainWindow)) == System.Windows.Forms.DialogResult.OK)
                {
                    this.AddRootFolder(dialog.SelectedPath);
                }
            }
            finally
            {
                dialog.Dispose();
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

    }
}
