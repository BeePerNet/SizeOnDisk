// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ResxLocalizationProvider
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using WPFLocalizeExtension.Engine;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// A singleton RESX provider that uses attached properties and the Parent property to iterate through the visual tree.
  /// </summary>
  public class ResxLocalizationProvider : ResxLocalizationProviderBase
  {
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultDictionary to set the fallback resource dictionary.
    /// </summary>
    public static readonly DependencyProperty DefaultDictionaryProperty = DependencyProperty.RegisterAttached("DefaultDictionary", typeof (string), typeof (ResxLocalizationProvider), new PropertyMetadata((object) null, new PropertyChangedCallback(ResxLocalizationProvider.DefaultDictionaryChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultAssembly to set the fallback assembly.
    /// </summary>
    public static readonly DependencyProperty DefaultAssemblyProperty = DependencyProperty.RegisterAttached("DefaultAssembly", typeof (string), typeof (ResxLocalizationProvider), new PropertyMetadata((object) null, new PropertyChangedCallback(ResxLocalizationProvider.DefaultAssemblyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> IgnoreCase to set the case sensitivity.
    /// </summary>
    public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.RegisterAttached("IgnoreCase", typeof (bool), typeof (ResxLocalizationProvider), new PropertyMetadata((object) true, new PropertyChangedCallback(ResxLocalizationProvider.IgnoreCaseChanged)));
    /// <summary>
    /// Lock object for the creation of the singleton instance.
    /// </summary>
    private static readonly object InstanceLock = new object();
    /// <summary>
    /// A dictionary for notification classes for changes of the individual target Parent changes.
    /// </summary>
    private readonly ParentNotifiers _parentNotifiers = new ParentNotifiers();
    /// <summary>The instance of the singleton.</summary>
    private static ResxLocalizationProvider _instance;

    /// <summary>
    /// Indicates, that the <see cref="F:WPFLocalizeExtension.Providers.ResxLocalizationProvider.DefaultDictionaryProperty" /> attached property changed.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="e">The event argument.</param>
    private static void DefaultDictionaryChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs e)
    {
      ResxLocalizationProvider.Instance.FallbackDictionary = e.NewValue?.ToString();
      ResxLocalizationProvider.Instance.OnProviderChanged(obj);
    }

    /// <summary>
    /// Indicates, that the <see cref="F:WPFLocalizeExtension.Providers.ResxLocalizationProvider.DefaultAssemblyProperty" /> attached property changed.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="e">The event argument.</param>
    private static void DefaultAssemblyChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs e)
    {
      ResxLocalizationProvider.Instance.FallbackAssembly = e.NewValue?.ToString();
      ResxLocalizationProvider.Instance.OnProviderChanged(obj);
    }

    /// <summary>
    /// Indicates, that the <see cref="F:WPFLocalizeExtension.Providers.ResxLocalizationProvider.IgnoreCaseProperty" /> attached property changed.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="e">The event argument.</param>
    private static void IgnoreCaseChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs e)
    {
      ResxLocalizationProvider.Instance.IgnoreCase = (bool) e.NewValue;
      ResxLocalizationProvider.Instance.OnProviderChanged(obj);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to get the default dictionary from.</param>
    /// <returns>The default dictionary.</returns>
    public static string GetDefaultDictionary(DependencyObject obj)
    {
      return obj.GetValueSync<string>(ResxLocalizationProvider.DefaultDictionaryProperty);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to get the default assembly from.</param>
    /// <returns>The default assembly.</returns>
    public static string GetDefaultAssembly(DependencyObject obj)
    {
      return obj.GetValueSync<string>(ResxLocalizationProvider.DefaultAssemblyProperty);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> ignore case flag.
    /// </summary>
    /// <param name="obj">The dependency object to get the ignore case flag from.</param>
    /// <returns>The ignore case flag.</returns>
    public static bool GetIgnoreCase(DependencyObject obj)
    {
      return obj.GetValueSync<bool>(ResxLocalizationProvider.IgnoreCaseProperty);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to set the default dictionary to.</param>
    /// <param name="value">The dictionary.</param>
    public static void SetDefaultDictionary(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(ResxLocalizationProvider.DefaultDictionaryProperty, value);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to set the default assembly to.</param>
    /// <param name="value">The assembly.</param>
    public static void SetDefaultAssembly(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(ResxLocalizationProvider.DefaultAssemblyProperty, value);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> ignore case flag.
    /// </summary>
    /// <param name="obj">The dependency object to set the ignore case flag to.</param>
    /// <param name="value">The ignore case flag.</param>
    public static void SetIgnoreCase(DependencyObject obj, bool value)
    {
      obj.SetValueSync<bool>(ResxLocalizationProvider.IgnoreCaseProperty, value);
    }

    /// <summary>To use when no assembly is specified.</summary>
    public string FallbackAssembly { get; set; }

    /// <summary>To use when no dictionary is specified.</summary>
    public string FallbackDictionary { get; set; }

    /// <summary>
    /// Gets the <see cref="T:WPFLocalizeExtension.Providers.ResxLocalizationProvider" /> singleton.
    /// </summary>
    public static ResxLocalizationProvider Instance
    {
      get
      {
        if (ResxLocalizationProvider._instance == null)
        {
          lock (ResxLocalizationProvider.InstanceLock)
          {
            if (ResxLocalizationProvider._instance == null)
              ResxLocalizationProvider._instance = new ResxLocalizationProvider();
          }
        }
        return ResxLocalizationProvider._instance;
      }
      set
      {
        lock (ResxLocalizationProvider.InstanceLock)
          ResxLocalizationProvider._instance = value;
      }
    }

    /// <summary>
    /// Resets the instance that is used for the ResxLocationProvider
    /// </summary>
    public static void Reset()
    {
      ResxLocalizationProvider.Instance = (ResxLocalizationProvider) null;
    }

    /// <summary>The singleton constructor.</summary>
    protected ResxLocalizationProvider()
    {
      this.ResourceManagerList = new Dictionary<string, ResourceManager>();
      ObservableCollection<CultureInfo> observableCollection = new ObservableCollection<CultureInfo>();
      observableCollection.Add(CultureInfo.InvariantCulture);
      this.AvailableCultures = observableCollection;
    }

    /// <summary>
    /// An action that will be called when a parent of one of the observed target objects changed.
    /// </summary>
    /// <param name="obj">The target <see cref="T:System.Windows.DependencyObject" />.</param>
    private void ParentChangedAction(DependencyObject obj)
    {
      this.OnProviderChanged(obj);
    }

    /// <summary>Get the assembly from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The assembly name, if available.</returns>
    protected override string GetAssembly(DependencyObject target)
    {
      if (target == null)
        return this.FallbackAssembly;
      string registerParentNotifier = target.GetValueOrRegisterParentNotifier<string>(ResxLocalizationProvider.DefaultAssemblyProperty, new Action<DependencyObject>(this.ParentChangedAction), this._parentNotifiers);
      if (!string.IsNullOrEmpty(registerParentNotifier))
        return registerParentNotifier;
      return this.FallbackAssembly;
    }

    /// <summary>Get the dictionary from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The dictionary name, if available.</returns>
    protected override string GetDictionary(DependencyObject target)
    {
      if (target == null)
        return this.FallbackDictionary;
      string registerParentNotifier = target.GetValueOrRegisterParentNotifier<string>(ResxLocalizationProvider.DefaultDictionaryProperty, new Action<DependencyObject>(this.ParentChangedAction), this._parentNotifiers);
      if (!string.IsNullOrEmpty(registerParentNotifier))
        return registerParentNotifier;
      return this.FallbackDictionary;
    }
  }
}
