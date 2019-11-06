using System;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.ViewModel
{
    public static class TaskHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<En attente>")]
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
