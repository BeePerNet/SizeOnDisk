using System;
using System.Globalization;
using System.Windows.Data;

namespace SizeOnDisk.Converters
{
    /// <summary>
    /// Check if enum value parameter is contained by the enum value
    /// </summary>
    public class EnumValueConverter : IValueConverter
    {
        /// <summary>
        /// Check if enum value parameter is contained by the enum value
        /// </summary>
        /// <param name="value">An enum value</param>
        /// <param name="parameter">An enum value name (string) to check</param>
        /// <returns>boolean</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;

            if (parameter == null)
                throw new ArgumentNullException("parameter", "cannot be null");

            if (!(parameter is IConvertible))
                throw new ArgumentOutOfRangeException(string.Format(culture, "Parameter must be of type IConvertible. parameter type: {0}", parameter.GetType()));

            IConvertible myEnumVal = (IConvertible)value;
            IConvertible myEnumValToCheck = (IConvertible)Enum.Parse(value.GetType(), parameter.ToString(), true);

            return (myEnumVal.ToInt64(CultureInfo.InvariantCulture) & myEnumValToCheck.ToInt64(CultureInfo.InvariantCulture)) > 0;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
