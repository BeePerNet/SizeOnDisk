// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.LocProxy
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.ComponentModel;
using System.Windows;
using WPFLocalizeExtension.Extensions;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>A proxy class to localize object strings.</summary>
  public class LocProxy : FrameworkElement
  {
    /// <summary>The source.</summary>
    public static DependencyProperty SourceProperty = DependencyProperty.Register(nameof (Source), typeof (object), typeof (LocProxy), new PropertyMetadata(new PropertyChangedCallback(LocProxy.PropertiesChanged)));
    /// <summary>
    /// This flag determines, if the type should be added using the given separator.
    /// </summary>
    public static DependencyProperty PrependTypeProperty = DependencyProperty.Register(nameof (PrependType), typeof (bool), typeof (LocProxy), new PropertyMetadata((object) false, new PropertyChangedCallback(LocProxy.PropertiesChanged)));
    /// <summary>The Separator.</summary>
    public static DependencyProperty SeparatorProperty = DependencyProperty.Register(nameof (Separator), typeof (string), typeof (LocProxy), new PropertyMetadata((object) "_", new PropertyChangedCallback(LocProxy.PropertiesChanged)));
    /// <summary>The Prefix.</summary>
    public static DependencyProperty PrefixProperty = DependencyProperty.Register(nameof (Prefix), typeof (string), typeof (LocProxy), new PropertyMetadata((object) null, new PropertyChangedCallback(LocProxy.PropertiesChanged)));
    /// <summary>The result.</summary>
    public static DependencyPropertyKey ResultProperty = DependencyProperty.RegisterReadOnly(nameof (Result), typeof (string), typeof (LocProxy), new PropertyMetadata((object) ""));
    /// <summary>
    /// Our own <see cref="T:WPFLocalizeExtension.Extensions.LocExtension" /> instance.
    /// </summary>
    private LocExtension _ext;

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.SourceProperty" />
    /// </summary>
    [Category("Common")]
    public object Source
    {
      get
      {
        return this.GetValue(LocProxy.SourceProperty);
      }
      set
      {
        this.SetValue(LocProxy.SourceProperty, value);
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
        return (bool) this.GetValue(LocProxy.PrependTypeProperty);
      }
      set
      {
        this.SetValue(LocProxy.PrependTypeProperty, (object) value);
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
        return (string) this.GetValue(LocProxy.SeparatorProperty);
      }
      set
      {
        this.SetValue(LocProxy.SeparatorProperty, (object) value);
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
        return (string) this.GetValue(LocProxy.PrefixProperty);
      }
      set
      {
        this.SetValue(LocProxy.PrefixProperty, (object) value);
      }
    }

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.ResultProperty" />
    /// </summary>
    [Category("Common")]
    public string Result
    {
      get
      {
        return (string) this.GetValue(LocProxy.ResultProperty.DependencyProperty) ?? this.Source.ToString();
      }
      set
      {
        this.SetValue(LocProxy.ResultProperty, (object) value);
      }
    }

    /// <summary>
    /// A notification handler for the <see cref="F:WPFLocalizeExtension.Engine.LocProxy.SourceProperty" />.
    /// </summary>
    /// <param name="d">The object.</param>
    /// <param name="e">The event arguments.</param>
    private static void PropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!(d is LocProxy locProxy))
        return;
      object source = locProxy.Source;
      if (source == null)
        return;
      string str = source.ToString();
      if (locProxy.PrependType)
        str = source.GetType().Name + locProxy.Separator + str;
      if (!string.IsNullOrEmpty(locProxy.Prefix))
        str = locProxy.Prefix + locProxy.Separator + str;
      if (locProxy._ext == null)
      {
        locProxy._ext = new LocExtension() { Key = str };
        locProxy._ext.SetBinding((DependencyObject) locProxy, (object) locProxy.GetType().GetProperty("Result"));
      }
      else
        locProxy._ext.Key = str;
    }
  }
}
