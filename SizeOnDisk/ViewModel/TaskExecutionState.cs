
using SizeOnDisk.Languages;
using System.ComponentModel.DataAnnotations;

namespace SizeOnDisk.ViewModel
{
    public enum TaskExecutionState : int
    {
        [Display(ResourceType = typeof(Localization), Name = nameof(Localization.Ready))]
        Ready = 0,
        [Display(ResourceType = typeof(Localization), Name = nameof(Localization.Calculating))]
        Running = 1,
        Canceling = 2,
        [Display(ResourceType = typeof(Localization), Name = nameof(Localization.Canceled))]
        Canceled = 3,
        [Display(ResourceType = typeof(Localization), Name = nameof(Localization.Finished))]
        Finished = 4,
        Designing = 42,
        Unknown = 31416
    }
}
