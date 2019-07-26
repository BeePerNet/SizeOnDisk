using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace SizeOnDisk.Converters
{
    public class FlowDocumentConverter : IValueConverter
    {
        private const string FlowDocumentMessage = "TargetType must be of type FlowDocument";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (targetType != typeof(FlowDocument))
                throw new ArgumentOutOfRangeException(nameof(targetType), FlowDocumentMessage);

            FlowDocument result;
            using (Stream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(value.ToString());
                    writer.Flush();
                    stream.Position = 0;
                    result = (FlowDocument)XamlReader.Load(stream);
                }
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return XamlWriter.Save(value);
        }
    }
}
