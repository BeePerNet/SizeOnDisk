// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.EnumRun
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using WPFLocalizeExtension.Extensions;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// An extension of <see cref="T:System.Windows.Documents.Run" /> for displaying localized enums.
  /// </summary>
  public class EnumRun : Run
  {
    /// <summary>The EnumValue.</summary>
    public static DependencyProperty EnumValueProperty = DependencyProperty.Register(nameof (EnumValue), typeof (Enum), typeof (EnumRun), new PropertyMetadata(new PropertyChangedCallback(EnumRun.PropertiesChanged)));
    /// <summary>
    /// This flag determines, if the type should be added using the given separator.
    /// </summary>
    public static DependencyProperty PrependTypeProperty = DependencyProperty.Register(nameof (PrependType), typeof (bool), typeof (EnumRun), new PropertyMetadata((object) false, new PropertyChangedCallback(EnumRun.PropertiesChanged)));
    /// <summary>The Separator.</summary>
    public static DependencyProperty SeparatorProperty = DependencyProperty.Register(nameof (Separator), typeof (string), typeof (EnumRun), new PropertyMetadata((object) "_", new PropertyChangedCallback(EnumRun.PropertiesChanged)));
    /// <summary>The Prefix.</summary>
    public static DependencyProperty PrefixProperty = DependencyProperty.Register(nameof (Prefix), typeof (string), typeof (EnumRun), new PropertyMetadata((object) null, new PropertyChangedCallback(EnumRun.PropertiesChanged)));
    /// <summary>
    /// Our own <see cref="T:WPFLocalizeExtension.Extensions.LocExtension" /> instance.
    /// </summary>
    private LocExtension _ext;

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.EnumRun.EnumValueProperty" />
    /// </summary>
    [Category("Common")]
    public Enum EnumValue
    {
      get
      {
        return (Enum) this.GetValue(EnumRun.EnumValueProperty);
      }
      set
      {
        this.SetValue(EnumRun.EnumValueProperty, (object) value);
      }
    }

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.PrependTypeProperty" />
    /// </summary>
    [Category("Common")]
    public bool PrependType
    {
      get
      {
        return (bool) this.GetValue(EnumRun.PrependTypeProperty);
      }
      set
      {
        this.SetValue(EnumRun.PrependTypeProperty, (object) value);
      }
    }

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.SeparatorProperty" />
    /// </summary>
    [Category("Common")]
    public string Separator
    {
      get
      {
        return (string) this.GetValue(EnumRun.SeparatorProperty);
      }
      set
      {
        this.SetValue(EnumRun.SeparatorProperty, (object) value);
      }
    }

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.PrefixProperty" />
    /// </summary>
    [Category("Common")]
    public string Prefix
    {
      get
      {
        return (string) this.GetValue(EnumRun.PrefixProperty);
      }
      set
      {
        this.SetValue(EnumRun.PrefixProperty, (object) value);
      }
    }

    /// <summary>A notification handler for changed properties.</summary>
    /// <param name="d">The object.</param>
    /// <param name="e">The event arguments.</param>
    private static void PropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!(d is EnumRun enumRun))
        return;
      Enum enumValue = enumRun.EnumValue;
      if (enumValue == null)
        return;
      string str = enumValue.ToString();
      if (enumRun.PrependType)
        str = enumValue.GetType().Name + enumRun.Separator + str;
      if (!string.IsNullOrEmpty(enumRun.Prefix))
        str = enumRun.Prefix + enumRun.Separator + str;
      if (enumRun._ext == null)
      {
        enumRun._ext = new LocExtension() { Key = str };
        enumRun._ext.SetBinding((DependencyObject) enumRun, (object) enumRun.GetType().GetProperty("Text"));
      }
      else
        enumRun._ext.Key = str;
    }
  }
}
