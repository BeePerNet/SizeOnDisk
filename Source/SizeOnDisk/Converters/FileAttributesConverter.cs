using SizeOnDisk.ViewModel;
using System;
using System.Text;
using System.Windows.Data;
using WPFByYourCommand.Converters;

namespace SizeOnDisk.Converters
{
    public class FileAttributesConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            FileAttributesEx attributes = (FileAttributesEx)value;

            if (targetType == typeof(string))
            {
                StringBuilder sb = new StringBuilder(15);
                if (((attributes & FileAttributesEx.ReadOnly) == FileAttributesEx.ReadOnly))
                {
                    sb.Append('R');
                }

                if (((attributes & FileAttributesEx.Hidden) == FileAttributesEx.Hidden))
                {
                    sb.Append('H');
                }

                if (((attributes & FileAttributesEx.System) == FileAttributesEx.System))
                {
                    sb.Append('S');
                }

                if (((attributes & FileAttributesEx.Archive) == FileAttributesEx.Archive))
                {
                    sb.Append('A');
                }

                if (((attributes & FileAttributesEx.Device) == FileAttributesEx.Device))
                {
                    sb.Append('D');
                }

                if (((attributes & FileAttributesEx.Normal) == FileAttributesEx.Normal))
                {
                    sb.Append('N');
                }

                if (((attributes & FileAttributesEx.Temporary) == FileAttributesEx.Temporary))
                {
                    sb.Append('T');
                }

                if (((attributes & FileAttributesEx.SparseFile) == FileAttributesEx.SparseFile))
                {
                    sb.Append("Sf");
                }

                if (((attributes & FileAttributesEx.ReparsePoint) == FileAttributesEx.ReparsePoint))
                {
                    sb.Append("Rp");
                }

                if (((attributes & FileAttributesEx.Compressed) == FileAttributesEx.Compressed))
                {
                    sb.Append('C');
                }

                if (((attributes & FileAttributesEx.Offline) == FileAttributesEx.Offline))
                {
                    sb.Append('O');
                }

                if (((attributes & FileAttributesEx.NotContentIndexed) == FileAttributesEx.NotContentIndexed))
                {
                    sb.Append('I');
                }

                if (((attributes & FileAttributesEx.Encrypted) == FileAttributesEx.Encrypted))
                {
                    sb.Append('E');
                }

                return sb.ToString();
            }
            return null;
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
