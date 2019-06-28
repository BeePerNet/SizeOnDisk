// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.LocBinding
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Windows;
using System.Windows.Data;
using WPFLocalizeExtension.Extensions;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// A binding proxy class that accepts bindings and forwards them to the LocExtension.
  /// Based on: http://www.codeproject.com/Articles/71348/Binding-on-a-Property-which-is-not-a-DependencyPro
  /// </summary>
  public class LocBinding : FrameworkElement
  {
    /// <summary>
    /// We don't know what will be the Source/target type so we keep 'object'.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof (Source), typeof (object), typeof (LocBinding), (PropertyMetadata) new FrameworkPropertyMetadata(new PropertyChangedCallback(LocBinding.OnPropertyChanged))
    {
      BindsTwoWayByDefault = true,
      DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
    });
    private LocExtension _target;

    /// <summary>The source.</summary>
    public object Source
    {
      get
      {
        return this.GetValue(LocBinding.SourceProperty);
      }
      set
      {
        this.SetValue(LocBinding.SourceProperty, value);
      }
    }

    /// <summary>The target extension.</summary>
    public LocExtension Target
    {
      get
      {
        return this._target;
      }
      set
      {
        this._target = value;
        if (this._target == null || this.Source == null)
          return;
        this._target.Key = this.Source.ToString();
      }
    }

    private static void OnPropertyChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs args)
    {
      if (!(obj is LocBinding locBinding) || args.Property != LocBinding.SourceProperty || (locBinding.Source == locBinding._target || locBinding._target == null) || locBinding.Source == null)
        return;
      locBinding._target.Key = locBinding.Source.ToString();
    }
  }
}
