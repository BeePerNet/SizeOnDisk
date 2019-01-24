using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public interface ICommandContext
    {
        CommandBindingCollection Commands { get; }
        InputBindingCollection Inputs { get; }
    }
}
