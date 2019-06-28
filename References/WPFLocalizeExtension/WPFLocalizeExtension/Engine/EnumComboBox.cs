// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.EnumComboBox
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// An extended combobox that is enumerating Enum values.
  /// <para>Use the <see cref="T:System.ComponentModel.BrowsableAttribute" /> to hide specific entries.</para>
  /// </summary>
  public class EnumComboBox : ComboBox
  {
    /// <summary>The Type.</summary>
    public static DependencyProperty TypeProperty = DependencyProperty.Register(nameof (Type), typeof (Type), typeof (EnumComboBox), new PropertyMetadata(new PropertyChangedCallback(EnumComboBox.TypeChanged)));
    /// <summary>
    /// This flag determines, if the type should be added using the given separator.
    /// </summary>
    public static DependencyProperty PrependTypeProperty = DependencyProperty.Register(nameof (PrependType), typeof (bool), typeof (EnumComboBox), new PropertyMetadata((object) false));
    /// <summary>The Separator.</summary>
    public static DependencyProperty SeparatorProperty = DependencyProperty.Register(nameof (Separator), typeof (string), typeof (EnumComboBox), new PropertyMetadata((object) "_"));
    /// <summary>The Prefix.</summary>
    public static DependencyProperty PrefixProperty = DependencyProperty.Register(nameof (Prefix), typeof (string), typeof (EnumComboBox), new PropertyMetadata((PropertyChangedCallback) null));
    private bool _shouldSerializeTemplate;

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.EnumComboBox.TypeProperty" />
    /// </summary>
    [Category("Common")]
    public Type Type
    {
      get
      {
        return (Type) this.GetValue(EnumComboBox.TypeProperty);
      }
      set
      {
        this.SetValue(EnumComboBox.TypeProperty, (object) value);
      }
    }

    private static void TypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!(d is EnumComboBox enumComboBox))
        return;
      enumComboBox.SetType(enumComboBox.Type);
    }

    /// <summary>
    /// The backing property for <see cref="F:WPFLocalizeExtension.Engine.LocProxy.PrependTypeProperty" />
    /// </summary>
    [Category("Common")]
    public bool PrependType
    {
      get
      {
        return (bool) this.GetValue(EnumComboBox.PrependTypeProperty);
      }
      set
      {
        this.SetValue(EnumComboBox.PrependTypeProperty, (object) value);
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
        return (string) this.GetValue(EnumComboBox.SeparatorProperty);
      }
      set
      {
        this.SetValue(EnumComboBox.SeparatorProperty, (object) value);
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
        return (string) this.GetValue(EnumComboBox.PrefixProperty);
      }
      set
      {
        this.SetValue(EnumComboBox.PrefixProperty, (object) value);
      }
    }

    /// <summary>Overwrite and bypass the Items property.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new ItemCollection Items
    {
      get
      {
        return base.Items;
      }
    }

    protected override void OnItemTemplateChanged(
      DataTemplate oldItemTemplate,
      DataTemplate newItemTemplate)
    {
      if (oldItemTemplate != null)
        this._shouldSerializeTemplate = true;
      base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);
    }

    protected override bool ShouldSerializeProperty(DependencyProperty dp)
    {
      if (dp == ItemsControl.ItemTemplateProperty && !this._shouldSerializeTemplate)
        return false;
      return base.ShouldSerializeProperty(dp);
    }

    private void SetType(Type type)
    {
      try
      {
        List<object> objectList = new List<object>();
        foreach (FieldInfo field in type.GetFields())
        {
          if (!field.IsSpecialName)
          {
            BrowsableAttribute browsableAttribute = field.GetCustomAttributes(false).OfType<BrowsableAttribute>().FirstOrDefault<BrowsableAttribute>();
            if (browsableAttribute == null || browsableAttribute.Browsable)
              objectList.Add(field.GetValue((object) 0));
          }
        }
        this.ItemsSource = (IEnumerable) objectList;
      }
      catch
      {
      }
    }

    /// <summary>Creates a new instance.</summary>
    public EnumComboBox()
    {
      this.ItemTemplate = (DataTemplate) XamlReader.Parse("<DataTemplate><TextBlock><lex:EnumRun EnumValue=\"{Binding}\"" + " PrependType=\"{Binding PrependType, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=lex:EnumComboBox}}\"" + " Separator=\"{Binding Separator, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=lex:EnumComboBox}}\"" + " Prefix=\"{Binding Prefix, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=lex:EnumComboBox}}\"" + " /></TextBlock></DataTemplate>", new ParserContext()
      {
        XmlnsDictionary = {
          {
            "",
            "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          },
          {
            "lex",
            "http://wpflocalizeextension.codeplex.com"
          }
        }
      });
    }
  }
}
