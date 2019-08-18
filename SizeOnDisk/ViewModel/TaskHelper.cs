using System;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.ViewModel
{
    public static class TaskHelper
    {
        public static Exception SafeExecute(Action action, bool showException)
        {
            try
            {
                if (action == null)
                {
                    throw new ArgumentNullException(nameof(action));
                }

                action();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (showException)
                {
                    ExceptionBox.ShowException(ex);
                }
                return ex;
            }
            return null;
        }

    }
}
