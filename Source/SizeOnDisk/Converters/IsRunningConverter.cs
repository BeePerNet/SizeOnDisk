using SizeOnDisk.Languages;
using SizeOnDisk.ViewModel;
using System;
using System.Windows.Data;
using System.Windows.Shell;

namespace SizeOnDisk.Converters
{
    public class IsRunningConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IConvertible convertible = value as IConvertible;
            if (targetType == typeof(TaskbarItemProgressState))
            {
                if (convertible != null && convertible.ToBoolean(culture))
                {
                    return TaskbarItemProgressState.Indeterminate;
                }

                return TaskbarItemProgressState.None;
            }
            if (targetType == typeof(bool))
            {
                if (value is TaskExecutionState)
                {
                    return ((TaskExecutionState)value) == TaskExecutionState.Running;
                }

                if (convertible != null && convertible.ToBoolean(culture))
                {
                    return true;
                }

                return false;
            }
            if (value != null && value is TaskExecutionState)
            {
                switch ((TaskExecutionState)value)
                {
                    case TaskExecutionState.Canceled:
                        return Localization.Canceled;
                    case TaskExecutionState.Finished:
                        return Localization.Finished;
                    case TaskExecutionState.Ready:
                        return Localization.Ready;
                    case TaskExecutionState.Running:
                        return Localization.Calculating;
                    case TaskExecutionState.Canceling:
                        return "Cancelling";
                    case TaskExecutionState.Unknown:
                        return "Unknown";
                    default:
                        return value.ToString();
                }
            }
            if (convertible != null && convertible.ToBoolean(culture))
            {
                return Localization.Calculating;
            }

            return Localization.Ready;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
