// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.LocTextExtension
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Windows.Markup;
using WPFLocalizeExtension.Engine;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Extensions
{
  [MarkupExtensionReturnType(typeof (string))]
  public class LocTextExtension : LocExtension
  {
    /// <summary>Holds the local format segment array</summary>
    private readonly string[] _formatSegments = new string[5];
    /// <summary>Holds the local prefix value</summary>
    private string _prefix;
    /// <summary>Holds the local suffix value</summary>
    private string _suffix;

    public LocTextExtension()
    {
    }

    public LocTextExtension(string key)
      : base(key)
    {
    }

    /// <summary>Gets or sets a prefix for the localized text</summary>
    public string Prefix
    {
      get
      {
        return this._prefix;
      }
      set
      {
        this._prefix = value;
      }
    }

    /// <summary>Gets or sets a suffix for the localized text</summary>
    public string Suffix
    {
      get
      {
        return this._suffix;
      }
      set
      {
        this._suffix = value;
      }
    }

    /// <summary>
    /// Gets or sets the format segment 1.
    /// This will be used to replace format place holders from the localized text.
    /// <see cref="T:WPFLocalizeExtension.Extensions.LocTextLowerExtension" /> and <see cref="T:WPFLocalizeExtension.Extensions.LocTextUpperExtension" /> will format this segment.
    /// </summary>
    /// <value>The format segment 1.</value>
    public string FormatSegment1
    {
      get
      {
        return this._formatSegments[0];
      }
      set
      {
        this._formatSegments[0] = value;
      }
    }

    /// <summary>
    /// Gets or sets the format segment 2.
    /// This will be used to replace format place holders from the localized text.
    /// <see cref="T:WPFLocalizeExtension.Extensions.LocTextUpperExtension" /> and <see cref="T:WPFLocalizeExtension.Extensions.LocTextLowerExtension" /> will format this segment.
    /// </summary>
    /// <value>The format segment 2.</value>
    public string FormatSegment2
    {
      get
      {
        return this._formatSegments[1];
      }
      set
      {
        this._formatSegments[1] = value;
      }
    }

    /// <summary>
    /// Gets or sets the format segment 3.
    /// This will be used to replace format place holders from the localized text.
    /// <see cref="T:WPFLocalizeExtension.Extensions.LocTextUpperExtension" /> and <see cref="T:WPFLocalizeExtension.Extensions.LocTextLowerExtension" /> will format this segment.
    /// </summary>
    /// <value>The format segment 3.</value>
    public string FormatSegment3
    {
      get
      {
        return this._formatSegments[2];
      }
      set
      {
        this._formatSegments[2] = value;
      }
    }

    /// <summary>
    /// Gets or sets the format segment 4.
    /// This will be used to replace format place holders from the localized text.
    /// <see cref="T:WPFLocalizeExtension.Extensions.LocTextUpperExtension" /> and <see cref="T:WPFLocalizeExtension.Extensions.LocTextLowerExtension" /> will format this segment.
    /// </summary>
    /// <value>The format segment 4.</value>
    public string FormatSegment4
    {
      get
      {
        return this._formatSegments[3];
      }
      set
      {
        this._formatSegments[3] = value;
      }
    }

    /// <summary>
    /// Gets or sets the format segment 5.
    /// This will be used to replace format place holders from the localized text.
    /// <see cref="T:WPFLocalizeExtension.Extensions.LocTextUpperExtension" /> and <see cref="T:WPFLocalizeExtension.Extensions.LocTextLowerExtension" /> will format this segment.
    /// </summary>
    /// <value>The format segment 5.</value>
    public string FormatSegment5
    {
      get
      {
        return this._formatSegments[4];
      }
      set
      {
        this._formatSegments[4] = value;
      }
    }

    /// <summary>
    /// Returns the prefix or suffix text, depending on the supplied <see cref="T:WPFLocalizeExtension.Extensions.LocTextExtension.TextAppendType" />.
    /// If the prefix or suffix is null, it will be returned a string.empty.
    /// </summary>
    /// <param name="at">The <see cref="T:WPFLocalizeExtension.Extensions.LocTextExtension.TextAppendType" /> defines the format of the return value</param>
    /// <returns>Returns the formated prefix or suffix</returns>
    private string GetAppendText(LocTextExtension.TextAppendType at)
    {
      string str = string.Empty;
      if (at == LocTextExtension.TextAppendType.Prefix && !string.IsNullOrEmpty(this._prefix))
        str = this._prefix ?? string.Empty;
      else if (at == LocTextExtension.TextAppendType.Suffix && !string.IsNullOrEmpty(this._suffix))
        str = this._suffix ?? string.Empty;
      return str;
    }

    /// <summary>
    /// This method formats the localized text.
    /// If the passed target text is null, string.empty will be returned.
    /// </summary>
    /// <param name="target">The text to format.</param>
    /// <returns>Returns the formated text or string.empty, if the target text was null.</returns>
    protected virtual string FormatText(string target)
    {
      return target ?? string.Empty;
    }

    /// <summary>
    /// This function returns the properly prepared output of the markup extension.
    /// </summary>
    /// <param name="info">Information about the target.</param>
    /// <param name="endPoint">Information about the endpoint.</param>
    public override object FormatOutput(TargetInfo endPoint, TargetInfo info)
    {
      string format = base.FormatOutput(endPoint, info) as string ?? string.Empty;
      string str;
      try
      {
        str = string.Format((IFormatProvider) LocalizeDictionary.Instance.SpecificCulture, format, (object) (this._formatSegments[0] ?? string.Empty), (object) (this._formatSegments[1] ?? string.Empty), (object) (this._formatSegments[2] ?? string.Empty), (object) (this._formatSegments[3] ?? string.Empty), (object) (this._formatSegments[4] ?? string.Empty));
      }
      catch (FormatException ex)
      {
        str = "TextFormatError: Max 5 Format PlaceHolders! {0} to {4}";
      }
      string appendText1 = this.GetAppendText(LocTextExtension.TextAppendType.Prefix);
      string appendText2 = this.GetAppendText(LocTextExtension.TextAppendType.Suffix);
      return (object) this.FormatText(appendText1 + str + appendText2);
    }

    /// <summary>
    /// This enumeration is used to determine the type
    /// of the return value of <see cref="M:WPFLocalizeExtension.Extensions.LocTextExtension.GetAppendText(WPFLocalizeExtension.Extensions.LocTextExtension.TextAppendType)" />
    /// </summary>
    protected enum TextAppendType
    {
      /// <summary>The return value is used as prefix</summary>
      Prefix,
      /// <summary>The return value is used as suffix</summary>
      Suffix,
    }
  }
}
