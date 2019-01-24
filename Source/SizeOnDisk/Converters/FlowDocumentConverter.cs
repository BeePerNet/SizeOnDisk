using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace SizeOnDisk.Converters
{
    public class FlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (targetType != typeof(FlowDocument))
                throw new ArgumentException("TargetType must be of type FlowDocument");

            FlowDocument result;
            using (Stream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(value.ToString());
                writer.Flush();
                stream.Position = 0;
                result = (FlowDocument)XamlReader.Load(stream);
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return XamlWriter.Save(value);
        }
    }
}
