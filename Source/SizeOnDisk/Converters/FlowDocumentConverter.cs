using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using WPFByYourCommand.Converters;

namespace SizeOnDisk.Converters
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<En attente>")]
    public class FlowDocumentConverter : BaseConverter, IValueConverter
    {
        private const string FlowDocumentMessage = "TargetType must be of type FlowDocument";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<En attente>")]
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType != typeof(FlowDocument))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), FlowDocumentMessage);
            }

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
