// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Extensions.BLoc
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WPFLocalizeExtension.Engine;

namespace WPFLocalizeExtension.Extensions
{
  /// <summary>
  /// A localization extension based on <see cref="T:System.Windows.Data.Binding" />.
  /// </summary>
  public class BLoc : Binding, INotifyPropertyChanged, IDictionaryEventListener, IDisposable
  {
    private static readonly object ResourceBufferLock = new object();
    private static Dictionary<string, object> _resourceBuffer = new Dictionary<string, object>();
    private object _value;
    private string _key;

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

    /// <summary>The value, the internal binding is pointing at.</summary>
    public object Value
    {
      get
      {
        return this._value;
      }
      set
      {
        if (this._value == value)
          return;
        this._value = value;
        this.RaisePropertyChanged(nameof (Value));
      }
    }

    /// <summary>Gets or sets the Key to a .resx object</summary>
    public string Key
    {
      get
      {
        return this._key;
      }
      set
      {
        if (!(this._key != value))
          return;
        this._key = value;
        this.UpdateNewValue();
        this.RaisePropertyChanged(nameof (Key));
      }
    }

    /// <summary>
    /// Gets or sets the culture to force a fixed localized object
    /// </summary>
    public string ForceCulture { get; set; }

    /// <summary>Clears the common resource buffer.</summary>
    public static void ClearResourceBuffer()
    {
      lock (BLoc.ResourceBufferLock)
      {
        BLoc._resourceBuffer?.Clear();
        BLoc._resourceBuffer = (Dictionary<string, object>) null;
      }
    }

    /// <summary>Adds an item to the resource buffer (threadsafe).</summary>
    /// <param name="key">The key.</param>
    /// <param name="item">The item.</param>
    internal static void SafeAddItemToResourceBuffer(string key, object item)
    {
      lock (BLoc.ResourceBufferLock)
      {
        if (LocalizeDictionary.Instance.DisableCache || BLoc._resourceBuffer.ContainsKey(key))
          return;
        BLoc._resourceBuffer.Add(key, item);
      }
    }

    /// <summary>
    /// Removes an item from the resource buffer (threadsafe).
    /// </summary>
    /// <param name="key">The key.</param>
    internal static void SafeRemoveItemFromResourceBuffer(string key)
    {
      lock (BLoc.ResourceBufferLock)
      {
        if (!BLoc._resourceBuffer.ContainsKey(key))
          return;
        BLoc._resourceBuffer.Remove(key);
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:WPFLocalizeExtension.Extensions.BLoc" /> class.
    /// </summary>
    public BLoc()
    {
      LocalizeDictionary.DictionaryEvent.AddListener((IDictionaryEventListener) this);
      this.Path = new PropertyPath(nameof (Value), new object[0]);
      this.Source = (object) this;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:WPFLocalizeExtension.Extensions.BLoc" /> class.
    /// </summary>
    /// <param name="key">The resource identifier.</param>
    public BLoc(string key)
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
    ~BLoc()
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
      this.Value = this.FormatOutput();
    }

    /// <summary>
    /// This function returns the properly prepared output of the markup extension.
    /// </summary>
    public object FormatOutput()
    {
      string qualifiedResourceKey = (string) LocalizeDictionary.Instance.GetFullyQualifiedResourceKey(this.Key, (DependencyObject) null);
      CultureInfo cultureOrDefault = this.GetForcedCultureOrDefault();
      string str = cultureOrDefault.Name + ":";
      object localizedObject;
      lock (BLoc.ResourceBufferLock)
      {
        if (BLoc._resourceBuffer.ContainsKey(str + qualifiedResourceKey))
        {
          localizedObject = BLoc._resourceBuffer[str + qualifiedResourceKey];
        }
        else
        {
          localizedObject = LocalizeDictionary.Instance.GetLocalizedObject(qualifiedResourceKey, (DependencyObject) null, cultureOrDefault);
          if (localizedObject == null)
            return (object) null;
          BLoc.SafeAddItemToResourceBuffer(str + qualifiedResourceKey, localizedObject);
        }
      }
      return localizedObject;
    }
  }
}
