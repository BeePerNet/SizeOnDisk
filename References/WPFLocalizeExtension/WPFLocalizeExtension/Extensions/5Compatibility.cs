// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.LocTextLowerExtension
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Windows.Markup;

namespace WPFLocalizeExtension.Extensions
{
  [MarkupExtensionReturnType(typeof (string))]
  public class LocTextLowerExtension : LocTextExtension
  {
    public LocTextLowerExtension()
    {
    }

    public LocTextLowerExtension(string key)
      : base(key)
    {
    }

    /// <summary>
    /// This method formats the localized text.
    /// If the passed target text is null, string.empty will be returned.
    /// </summary>
    /// <param name="target">The text to format.</param>
    /// <returns>
    /// Returns the formated text or string.empty, if the target text was null.
    /// </returns>
    protected override string FormatText(string target)
    {
      return target?.ToLower(this.GetForcedCultureOrDefault()) ?? string.Empty;
    }
  }
}
