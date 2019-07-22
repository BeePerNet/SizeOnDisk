
namespace SizeOnDisk.ViewModel
{
    public enum TaskExecutionState : int
    {
        Ready = 0,
        Running = 1,
        Canceling = 2,
        Canceled = 3,
        Finished = 4,
        Designing = 42
    }
}
