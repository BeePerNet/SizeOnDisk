// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.TypeConverters.BitmapSourceTypeConverter
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WPFLocalizeExtension.TypeConverters
{
  /// <summary>
  /// A type converter class for Bitmap resources that are used in WPF.
  /// </summary>
  public class BitmapSourceTypeConverter : TypeConverter
  {
    /// <summary>
    /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
    /// </summary>
    /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
    /// <param name="sourceType">A <see cref="T:System.Type" /> that represents the type you want to convert from.</param>
    /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      return sourceType == typeof (Bitmap);
    }

    /// <summary>
    /// Returns whether this converter can convert the object to the specified type, using the specified context.
    /// </summary>
    /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
    /// <param name="destinationType">A <see cref="T:System.Type" /> that represents the type you want to convert to.</param>
    /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      return destinationType == typeof (Bitmap);
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
      if (!(value is Bitmap bitmap))
        return (object) null;
      IntPtr hbitmap = bitmap.GetHbitmap();
      BitmapSource sourceFromHbitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      sourceFromHbitmap.Freeze();
      BitmapSourceTypeConverter.DeleteObject(hbitmap);
      return (object) sourceFromHbitmap;
    }

    /// <summary>Frees memory of a pointer.</summary>
    /// <param name="o">Object to remove from memory.</param>
    /// <returns>0 if the removing was success, otherwise another number.</returns>
    [DllImport("gdi32.dll")]
    private static extern int DeleteObject(IntPtr o);

    /// <summary>
    /// Converts the given value object to the specified type, using the specified context and culture information.
    /// </summary>
    /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
    /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to use as the current culture.</param>
    /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
    /// <param name="destinationType">The <see cref="T:System.Type" /> to convert the value parameter to.</param>
    /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
    public override object ConvertTo(
      ITypeDescriptorContext context,
      CultureInfo culture,
      object value,
      Type destinationType)
    {
      BitmapSource bitmapSource = value as BitmapSource;
      if (value == null)
        return (object) null;
      Bitmap bitmap = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, PixelFormat.Format32bppPArgb);
      BitmapData bitmapdata = bitmap.LockBits(new Rectangle(System.Drawing.Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
      bitmapSource.CopyPixels(Int32Rect.Empty, bitmapdata.Scan0, bitmapdata.Height * bitmapdata.Stride, bitmapdata.Stride);
      bitmap.UnlockBits(bitmapdata);
      return (object) bitmap;
    }
  }
}
