// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.LocDoubleExtension
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Windows.Markup;

namespace WPFLocalizeExtension.Extensions
{
  [MarkupExtensionReturnType(typeof (double))]
  public class LocDoubleExtension : LocExtension
  {
    public LocDoubleExtension()
    {
    }

    public LocDoubleExtension(string key)
      : base(key)
    {
    }
  }
}
