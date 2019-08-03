using SizeOnDisk.Utilities;
using System;

namespace SizeOnDisk.ViewModel
{
    public static class TaskHelper
    {
        public static Exception SafeExecute(Action action)
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
                return ex;
            }
            return null;
        }

    }
}
