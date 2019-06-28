// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.LocThicknessExtension
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Windows;
using System.Windows.Markup;

namespace WPFLocalizeExtension.Extensions
{
  [MarkupExtensionReturnType(typeof (Thickness))]
  public class LocThicknessExtension : LocExtension
  {
    public LocThicknessExtension()
    {
    }

    public LocThicknessExtension(string key)
      : base(key)
    {
    }
  }
}
