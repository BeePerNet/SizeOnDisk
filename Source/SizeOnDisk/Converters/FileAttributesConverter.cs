using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SizeOnDisk.Converters
{
    public class FileAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            FileAttributes attributes = (FileAttributes)value;

            if (targetType == typeof(string))
            {
                StringBuilder sb = new StringBuilder(15);
                if (((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                    sb.Append('R');
                if (((attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                    sb.Append('H');
                if (((attributes & FileAttributes.System) == FileAttributes.System))
                    sb.Append('S');
                if (((attributes & FileAttributes.Archive) == FileAttributes.Archive))
                    sb.Append('A');
                if (((attributes & FileAttributes.Device) == FileAttributes.Device))
                    sb.Append('D');
                if (((attributes & FileAttributes.Normal) == FileAttributes.Normal))
                    sb.Append('N');
                if (((attributes & FileAttributes.Temporary) == FileAttributes.Temporary))
                    sb.Append('T');
                if (((attributes & FileAttributes.SparseFile) == FileAttributes.SparseFile))
                    sb.Append("Sf");
                if (((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint))
                    sb.Append("Rp");
                if (((attributes & FileAttributes.Compressed) == FileAttributes.Compressed))
                    sb.Append('C');
                if (((attributes & FileAttributes.Offline) == FileAttributes.Offline))
                    sb.Append('O');
                if (((attributes & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed))
                    sb.Append('I');
                if (((attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted))
                    sb.Append('E');
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
