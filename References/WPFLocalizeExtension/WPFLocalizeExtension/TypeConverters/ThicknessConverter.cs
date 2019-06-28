// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.TypeConverters.ThicknessConverter
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace WPFLocalizeExtension.TypeConverters
{
  /// <summary>
  /// A converter for the type <see cref="T:System.Windows.Thickness" />.
  /// </summary>
  public class ThicknessConverter : TypeConverter
  {
    /// <summary>
    /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
    /// </summary>
    /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
    /// <param name="sourceType">A <see cref="T:System.Type" /> that represents the type you want to convert from.</param>
    /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      return sourceType == typeof (string);
    }

    /// <summary>
    /// Converts the given object to the type of this converter, using the specified context and culture information.
    /// </summary>
    /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
    /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to use as the current culture.</param>
    /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
    /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
    public override object ConvertFrom(
      ITypeDescriptorContext context,
      CultureInfo culture,
      object value)
    {
      Thickness thickness = new Thickness();
      if (value is string str)
      {
        string[] strArray = str.Split(",".ToCharArray());
        switch (strArray.Length)
        {
          case 1:
            double result1;
            double.TryParse(strArray[0], NumberStyles.Any, (IFormatProvider) culture, out result1);
            thickness = new Thickness(result1);
            break;
          case 2:
            double result2;
            double.TryParse(strArray[0], NumberStyles.Any, (IFormatProvider) culture, out result2);
            double result3;
            double.TryParse(strArray[1], NumberStyles.Any, (IFormatProvider) culture, out result3);
            thickness = new Thickness(result2, result3, result2, result3);
            break;
          case 4:
            double result4;
            double.TryParse(strArray[0], NumberStyles.Any, (IFormatProvider) culture, out result4);
            double result5;
            double.TryParse(strArray[1], NumberStyles.Any, (IFormatProvider) culture, out result5);
            double result6;
            double.TryParse(strArray[2], NumberStyles.Any, (IFormatProvider) culture, out result6);
            double result7;
            double.TryParse(strArray[3], NumberStyles.Any, (IFormatProvider) culture, out result7);
            thickness = new Thickness(result4, result5, result6, result7);
            break;
        }
      }
      return (object) thickness;
    }
  }
}
