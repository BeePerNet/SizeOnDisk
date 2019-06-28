// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.TypeConverters.DefaultConverter
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFLocalizeExtension.TypeConverters
{
  /// <summary>
  /// Implements a standard converter that calls itself all known type converters.
  /// </summary>
  public class DefaultConverter : IValueConverter
  {
    private static readonly Dictionary<Type, TypeConverter> TypeConverters = new Dictionary<Type, TypeConverter>();

    /// <summary>
    /// Modifies the source data before passing it to the target for display in the UI.
    /// </summary>
    /// <param name="value">The source data being passed to the target.</param>
    /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the target dependency property.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
    /// <param name="culture">The culture of the conversion.</param>
    /// <returns>The value to be passed to the target dependency property.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null)
        return (object) null;
      Type type = value.GetType();
      if (targetType == typeof (object) || type == targetType)
        return value;
      RegisterMissingTypeConverters.Register();
      if (!DefaultConverter.TypeConverters.ContainsKey(targetType))
      {
        TypeConverter typeConverter = TypeDescriptor.GetConverter(targetType);
        if (targetType == typeof (Thickness))
          typeConverter = (TypeConverter) new ThicknessConverter();
        DefaultConverter.TypeConverters.Add(targetType, typeConverter);
      }
      TypeConverter typeConverter1 = DefaultConverter.TypeConverters[targetType];
      if (typeConverter1 != null)
      {
        if (typeConverter1.CanConvertFrom(type))
        {
          try
          {
            return typeConverter1.ConvertFrom(value);
          }
          catch
          {
            return (object) null;
          }
        }
      }
      return (object) null;
    }

    /// <summary>
    /// Modifies the target data before passing it to the source object.
    /// </summary>
    /// <param name="value">The target data being passed to the source.</param>
    /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the source object.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
    /// <param name="culture">The culture of the conversion.</param>
    /// <returns>The value to be passed to the source object.</returns>
    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      return this.Convert(value, targetType, parameter, culture);
    }
  }
}
