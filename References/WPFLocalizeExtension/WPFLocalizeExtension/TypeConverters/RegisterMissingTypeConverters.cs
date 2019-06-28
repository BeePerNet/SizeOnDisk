// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.TypeConverters.RegisterMissingTypeConverters
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace WPFLocalizeExtension.TypeConverters
{
  /// <summary>Register missing type converters here.</summary>
  public static class RegisterMissingTypeConverters
  {
    /// <summary>A flag indication if the registration was successful.</summary>
    private static bool _registered;

    /// <summary>Registers the missing type converters.</summary>
    public static void Register()
    {
      if (RegisterMissingTypeConverters._registered)
        return;
      TypeDescriptor.AddAttributes(typeof (BitmapSource), (Attribute) new TypeConverterAttribute(typeof (BitmapSourceTypeConverter)));
      RegisterMissingTypeConverters._registered = true;
    }
  }
}
