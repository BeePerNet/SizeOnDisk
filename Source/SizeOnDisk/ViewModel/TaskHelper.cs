using SizeOnDisk.Utilities;
using System;

namespace SizeOnDisk.ViewModel
{
    public static class TaskHelper
    {
        public static void SafeExecute(Action action)
        {
            try
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                action();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
        }

    }
}
