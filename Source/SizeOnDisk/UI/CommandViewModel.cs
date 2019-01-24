using System.ComponentModel;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public abstract class CommandViewModel : INotifyPropertyChanged, ICommandContext
    {
        CommandBindingCollection _commandList;
        private static string _statusText = string.Empty;

        public CommandBindingCollection Commands
        {
            get
            {
                if (_commandList == null)
                {
                    _commandList = new CommandBindingCollection();
                    this.AddCommandModels(_commandList);
                }

                return _commandList;
            }
        }

        public abstract void AddCommandModels(CommandBindingCollection bindingCollection);

        InputBindingCollection _inputList;

        public InputBindingCollection Inputs
        {
            get
            {
                if (_inputList == null)
                {
                    _inputList = new InputBindingCollection();
                    this.AddInputModels(_inputList);
                }

                return _inputList;
            }
        }

        public abstract void AddInputModels(InputBindingCollection bindingCollection);

        ///<summary>
        ///PropertyChanged event for INotifyPropertyChanged implementation.
        ///</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    this.OnPropertyChanged("StatusText");
                }
            }
        }
    }
}
