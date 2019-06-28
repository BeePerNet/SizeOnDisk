// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.FELoc
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.TypeConverters;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Extensions
{
  /// <summary>
  /// A localization utility based on <see cref="T:System.Windows.FrameworkElement" />.
  /// </summary>
  public class FELoc : FrameworkElement, IDictionaryEventListener, INotifyPropertyChanged, IDisposable
  {
    private static readonly object ResourceBufferLock = new object();
    private static Dictionary<string, object> _resourceBuffer = new Dictionary<string, object>();
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> Key to set the resource key.
    /// </summary>
    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(nameof (Key), typeof (string), typeof (FELoc), new PropertyMetadata((object) null, new PropertyChangedCallback(FELoc.DependencyPropertyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> Converter to set the <see cref="T:System.Windows.Data.IValueConverter" /> used to adapt to the target.
    /// </summary>
    public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(nameof (Converter), typeof (IValueConverter), typeof (FELoc), new PropertyMetadata((object) new DefaultConverter(), new PropertyChangedCallback(FELoc.DependencyPropertyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> ConverterParameter.
    /// </summary>
    public static readonly DependencyProperty ConverterParameterProperty = DependencyProperty.Register(nameof (ConverterParameter), typeof (object), typeof (FELoc), new PropertyMetadata((object) null, new PropertyChangedCallback(FELoc.DependencyPropertyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> ForceCulture.
    /// </summary>
    public static readonly DependencyProperty ForceCultureProperty = DependencyProperty.Register(nameof (ForceCulture), typeof (string), typeof (FELoc), new PropertyMetadata((object) null, new PropertyChangedCallback(FELoc.DependencyPropertyChanged)));
    private ParentChangedNotifier _parentChangedNotifier;
    private TargetInfo _targetInfo;
    private object _content;

    /// <summary>Informiert über sich ändernde Eigenschaften.</summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>Notify that a property has changed</summary>
    /// <param name="property">The property that changed</param>
    internal void RaisePropertyChanged(string property)
    {
      PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
      if (propertyChanged == null)
        return;
      propertyChanged((object) this, new PropertyChangedEventArgs(property));
    }

    /// <summary>Clears the common resource buffer.</summary>
    public static void ClearResourceBuffer()
    {
      lock (FELoc.ResourceBufferLock)
      {
        FELoc._resourceBuffer?.Clear();
        FELoc._resourceBuffer = (Dictionary<string, object>) null;
      }
    }

    /// <summary>Adds an item to the resource buffer (threadsafe).</summary>
    /// <param name="key">The key.</param>
    /// <param name="item">The item.</param>
    internal static void SafeAddItemToResourceBuffer(string key, object item)
    {
      lock (FELoc.ResourceBufferLock)
      {
        if (LocalizeDictionary.Instance.DisableCache || FELoc._resourceBuffer.ContainsKey(key))
          return;
        FELoc._resourceBuffer.Add(key, item);
      }
    }

    /// <summary>
    /// Removes an item from the resource buffer (threadsafe).
    /// </summary>
    /// <param name="key">The key.</param>
    internal static void SafeRemoveItemFromResourceBuffer(string key)
    {
      lock (FELoc.ResourceBufferLock)
      {
        if (!FELoc._resourceBuffer.ContainsKey(key))
          return;
        FELoc._resourceBuffer.Remove(key);
      }
    }

    /// <summary>The resource key.</summary>
    public string Key
    {
      get
      {
        return this.GetValueSync<string>(FELoc.KeyProperty);
      }
      set
      {
        this.SetValueSync<string>(FELoc.KeyProperty, value);
      }
    }

    /// <summary>Gets or sets the custom value converter.</summary>
    public IValueConverter Converter
    {
      get
      {
        return this.GetValueSync<IValueConverter>(FELoc.ConverterProperty);
      }
      set
      {
        this.SetValueSync<IValueConverter>(FELoc.ConverterProperty, value);
      }
    }

    /// <summary>Gets or sets the converter parameter.</summary>
    public object ConverterParameter
    {
      get
      {
        return this.GetValueSync<object>(FELoc.ConverterParameterProperty);
      }
      set
      {
        this.SetValueSync<object>(FELoc.ConverterParameterProperty, value);
      }
    }

    /// <summary>Gets or sets the forced culture.</summary>
    public string ForceCulture
    {
      get
      {
        return this.GetValueSync<string>(FELoc.ForceCultureProperty);
      }
      set
      {
        this.SetValueSync<string>(FELoc.ForceCultureProperty, value);
      }
    }

    /// <summary>Gets or sets the content.</summary>
    public object Content
    {
      get
      {
        return this._content;
      }
      set
      {
        if (this._content == value)
          return;
        this._content = value;
        this.RaisePropertyChanged(nameof (Content));
      }
    }

    /// <summary>Indicates, that the key changed.</summary>
    /// <param name="obj">The FELoc object.</param>
    /// <param name="args">The event argument.</param>
    private static void DependencyPropertyChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs args)
    {
      if (!(obj is FELoc feLoc))
        return;
      feLoc.UpdateNewValue();
    }

    private IList<DependencyProperty> GetAttachedProperties(
      DependencyObject obj)
    {
      List<DependencyProperty> dependencyPropertyList = new List<DependencyProperty>();
      foreach (PropertyDescriptor property in TypeDescriptor.GetProperties((object) obj, new Attribute[1]
      {
        (Attribute) new PropertyFilterAttribute(PropertyFilterOptions.All)
      }))
      {
        DependencyPropertyDescriptor propertyDescriptor = DependencyPropertyDescriptor.FromProperty(property);
        if (propertyDescriptor != null && propertyDescriptor.IsAttached)
          dependencyPropertyList.Add(propertyDescriptor.DependencyProperty);
      }
      return (IList<DependencyProperty>) dependencyPropertyList;
    }

    /// <summary>
    /// Based on http://social.msdn.microsoft.com/Forums/en/wpf/thread/580234cb-e870-4af1-9a91-3e3ba118c89c
    /// </summary>
    /// <param name="element">The target object.</param>
    /// <returns>The list of DependencyProperties of the object.</returns>
    private IEnumerable<DependencyProperty> GetDependencyProperties(
      object element)
    {
      List<DependencyProperty> dependencyPropertyList = new List<DependencyProperty>();
      foreach (MarkupProperty property in MarkupWriter.GetMarkupObjectFor(element).Properties)
      {
        if (property.DependencyProperty != null)
          dependencyPropertyList.Add(property.DependencyProperty);
      }
      return (IEnumerable<DependencyProperty>) dependencyPropertyList;
    }

    private void RegisterParentNotifier()
    {
      this._parentChangedNotifier = new ParentChangedNotifier((FrameworkElement) this, (Action) (() =>
      {
        this._parentChangedNotifier.Dispose();
        this._parentChangedNotifier = (ParentChangedNotifier) null;
        DependencyObject parent = this.Parent;
        if (parent == null)
          return;
        foreach (DependencyProperty dependencyProperty in this.GetDependencyProperties((object) parent))
        {
          if (parent.GetValue(dependencyProperty) == this)
          {
            this._targetInfo = new TargetInfo((object) parent, (object) dependencyProperty, dependencyProperty.PropertyType, -1);
            Binding binding = new Binding("Content")
            {
              Source = (object) this,
              Converter = this.Converter,
              ConverterParameter = this.ConverterParameter,
              Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(parent, dependencyProperty, (BindingBase) binding);
            this.UpdateNewValue();
          }
        }
      }));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:WPFLocalizeExtension.Extensions.BLoc" /> class.
    /// </summary>
    public FELoc()
    {
      LocalizeDictionary.DictionaryEvent.AddListener((IDictionaryEventListener) this);
      this.RegisterParentNotifier();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:WPFLocalizeExtension.Extensions.BLoc" /> class.
    /// </summary>
    /// <param name="key">The resource identifier.</param>
    public FELoc(string key)
      : this()
    {
      this.Key = key;
    }

    /// <summary>Removes the listener from the dictionary.</summary>
    public void Dispose()
    {
      LocalizeDictionary.DictionaryEvent.RemoveListener((IDictionaryEventListener) this);
    }

    /// <summary>The finalizer.</summary>
    ~FELoc()
    {
      this.Dispose();
    }

    /// <summary>
    /// If Culture property defines a valid <see cref="T:System.Globalization.CultureInfo" />, a <see cref="T:System.Globalization.CultureInfo" /> instance will get
    /// created and returned, otherwise <see cref="T:WPFLocalizeExtension.Engine.LocalizeDictionary" />.Culture will get returned.
    /// </summary>
    /// <returns>The <see cref="T:System.Globalization.CultureInfo" /></returns>
    /// <exception cref="T:System.ArgumentException">
    /// thrown if the parameter Culture don't defines a valid <see cref="T:System.Globalization.CultureInfo" />
    /// </exception>
    protected CultureInfo GetForcedCultureOrDefault()
    {
      CultureInfo cultureInfo;
      if (!string.IsNullOrEmpty(this.ForceCulture))
      {
        try
        {
          cultureInfo = new CultureInfo(this.ForceCulture);
        }
        catch (ArgumentException ex)
        {
          if (!LocalizeDictionary.Instance.GetIsInDesignMode())
            throw new ArgumentException("Cannot create a CultureInfo with '" + this.ForceCulture + "'", (Exception) ex);
          cultureInfo = LocalizeDictionary.Instance.SpecificCulture;
        }
      }
      else
        cultureInfo = LocalizeDictionary.Instance.SpecificCulture;
      return cultureInfo;
    }

    /// <summary>
    /// This method is called when the resource somehow changed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    public void ResourceChanged(DependencyObject sender, DictionaryEventArgs e)
    {
      this.UpdateNewValue();
    }

    private void UpdateNewValue()
    {
      this.Content = this.FormatOutput();
    }

    /// <summary>
    /// This function returns the properly prepared output of the markup extension.
    /// </summary>
    public object FormatOutput()
    {
      object obj1 = (object) null;
      if (this._targetInfo == null)
        return (object) null;
      DependencyObject targetObject = this._targetInfo.TargetObject as DependencyObject;
      Type targetType = this._targetInfo.TargetPropertyType;
      if (targetType == typeof (ImageSource))
        targetType = typeof (BitmapSource);
      string qualifiedResourceKey1 = (string) LocalizeDictionary.Instance.GetFullyQualifiedResourceKey(this.Key, targetObject);
      CultureInfo cultureOrDefault = this.GetForcedCultureOrDefault();
      string key1 = "";
      string str = "";
      if (targetObject is FrameworkElement frameworkElement)
        key1 = frameworkElement.GetValueSync<string>(FrameworkElement.NameProperty);
      else if (targetObject is FrameworkContentElement)
        key1 = targetObject.GetValueSync<string>(FrameworkContentElement.NameProperty);
      if (this._targetInfo.TargetProperty is PropertyInfo targetProperty)
        str = targetProperty.Name;
      else if (this._targetInfo.TargetProperty is DependencyProperty)
        str = ((DependencyProperty) this._targetInfo.TargetProperty).Name;
      if (str.Contains("FrameworkElementWidth5"))
        str = "Height";
      else if (str.Contains("FrameworkElementWidth6"))
        str = "Width";
      else if (str.Contains("FrameworkElementMargin12"))
        str = "Margin";
      string key2 = cultureOrDefault.Name + ":" + targetType.Name + ":";
      string qualifiedResourceKey2 = (string) LocalizeDictionary.Instance.GetFullyQualifiedResourceKey(key1 + LocalizeDictionary.GetSeparation(targetObject) + str, targetObject);
      string qualifiedResourceKey3 = (string) LocalizeDictionary.Instance.GetFullyQualifiedResourceKey(key1, targetObject);
      object obj2 = (object) null;
      if (!string.IsNullOrEmpty(qualifiedResourceKey1))
      {
        lock (FELoc.ResourceBufferLock)
        {
          if (FELoc._resourceBuffer.ContainsKey(key2 + qualifiedResourceKey1))
          {
            obj1 = FELoc._resourceBuffer[key2 + qualifiedResourceKey1];
          }
          else
          {
            obj2 = LocalizeDictionary.Instance.GetLocalizedObject(qualifiedResourceKey1, targetObject, cultureOrDefault);
            key2 += qualifiedResourceKey1;
          }
        }
      }
      else
      {
        lock (FELoc.ResourceBufferLock)
        {
          if (FELoc._resourceBuffer.ContainsKey(key2 + qualifiedResourceKey2))
          {
            obj1 = FELoc._resourceBuffer[key2 + qualifiedResourceKey2];
          }
          else
          {
            obj2 = LocalizeDictionary.Instance.GetLocalizedObject(qualifiedResourceKey2, targetObject, cultureOrDefault);
            if (obj2 == null)
            {
              if (FELoc._resourceBuffer.ContainsKey(key2 + qualifiedResourceKey3))
              {
                obj1 = FELoc._resourceBuffer[key2 + qualifiedResourceKey3];
              }
              else
              {
                obj2 = LocalizeDictionary.Instance.GetLocalizedObject(qualifiedResourceKey3, targetObject, cultureOrDefault);
                key2 += qualifiedResourceKey3;
              }
            }
            else
              key2 += qualifiedResourceKey2;
          }
        }
      }
      if (obj1 == null && obj2 != null)
      {
        obj1 = this.Converter.Convert(obj2, targetType, this.ConverterParameter, cultureOrDefault);
        FELoc.SafeAddItemToResourceBuffer(key2, obj1);
      }
      return obj1;
    }
  }
}
