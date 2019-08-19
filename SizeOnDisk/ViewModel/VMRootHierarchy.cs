using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMRootHierarchy : CommandViewModel
    {
        public ObservableImmutableCollection<VMRootFolder> Folders { get; } = new ObservableImmutableCollection<VMRootFolder>();

        private void TimerTick(object sender, EventArgs e)
        {
            RunningThreads = Process.GetCurrentProcess().Threads.Count;
        }



        private int _RunningThreads = 0;
        public int RunningThreads
        {
            get => _RunningThreads;
            set => SetProperty(ref _RunningThreads, value);
        }

        private VMRootFolder _SelectedRootFolder;

        public VMRootFolder SelectedRootFolder
        {
            get => _SelectedRootFolder;
            set => SetProperty(ref _SelectedRootFolder, value);
        }

        private readonly DispatcherTimer _Timer;

        public VMRootHierarchy()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                VMRootFolder newFolder = new VMRootFolder(this);
                Folders.Add(newFolder);
                SelectedRootFolder = newFolder;
            }
            else
            {
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
            string name = path;
            if (name.Count(T => T == '\\') > 1)
            {
                name = Path.GetFileName(path);
            }

            VMRootFolder newFolder = new VMRootFolder(this, name, path);
            Folders.Add(newFolder);
            SelectedRootFolder = newFolder;
            newFolder.RefreshAsync();
        }

        public void RemoveRootFolder(VMRootFolder folder)
        {
            folder.Stop().Wait();
            Folders.Remove(folder);
            if (SelectedRootFolder == null || SelectedRootFolder == folder)
            {
                SelectedRootFolder = Folders.FirstOrDefault();
            }
        }

        public void StopAllAsync()
        {
            Task[] tasks = Folders.Select(T => (T as VMRootFolder).Stop()).Where(T => T != null).ToArray();
            if (tasks.Length > 0)
            {
                Task.WaitAll(tasks);
            }
        }

        public VMFile FindVMFile(string path)
        {
            return Folders.FirstOrDefault(T => path.StartsWith(T.Path, StringComparison.CurrentCultureIgnoreCase))?.FindVMFile(path);
        }


        #endregion function

        #region Commands

        public bool IsRunning => Folders.Cast<VMRootFolder>().Any(T => T.ExecutionState == TaskExecutionState.Running);

        internal void RefreshIsRunning()
        {
            OnPropertyChanged(nameof(IsRunning));
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
            {
                throw new ArgumentNullException(nameof(bindingCollection));
            }

            bindingCollection.Add(new CommandBinding(OpenFolderCommand, CallOpenCommand));
            bindingCollection.Add(new CommandBinding(RefreshAllCommand, CallRefreshCommand, CanCallRefreshCommand));
        }

        private void CallOpenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            OpenFolderDialog dialog = new OpenFolderDialog();
            if (dialog.ShowDialog(new WrapperIWin32Window(Application.Current.MainWindow)))
            {
                AddRootFolder(dialog.Folder);
            }
        }


        private void CanCallRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = Folders.Count > 0;
        }

        private void CallRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            foreach (VMRootFolder folder in Folders)
            {
                folder.RefreshAsync();
            }
        }

        public override void AddInputModels(InputBindingCollection bindingCollection)
        {
        }

        #endregion Commands

    }
}
