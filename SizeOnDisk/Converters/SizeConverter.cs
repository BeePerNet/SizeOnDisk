using System;
using System.Windows.Data;
using WPFByYourCommand.Converters;

namespace SizeOnDisk.Converters
{
    public class SizeConverter : BaseConverter, IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is IConvertible)
            {
                //Format by factor 1024 K
                ulong size = System.Convert.ToUInt64(value, culture);
                switch (Properties.Settings.Default.UISizeFormat)
                {
                    case UISizeFormatType.KBytes:
                        if (size < 1024 && size > 0)
                        {
                            return (size / 1024D).ToString("N3", culture);
                        }
                        size /= 1024;
                        break;
                    case UISizeFormatType.FactorBy1024:
                        byte factor = 0;
                        while (size > 1024)
                        {
                            size /= 1024;
                            factor++;
                        }
                        return string.Concat(size.ToString("N0", culture), " ", Enum.GetName(typeof(IOSizeSuffixFactor), factor));
                }
                return size.ToString("N0", culture);
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private enum IOSizeSuffixFactor : byte
        {
            o = 0,
            Ko = 1,
            Mo = 2,
            Go = 3,
            To = 4,
            Po = 5,
            Eo = 6,
            Zo = 7,
            Yo = 8
        }
    }
}
