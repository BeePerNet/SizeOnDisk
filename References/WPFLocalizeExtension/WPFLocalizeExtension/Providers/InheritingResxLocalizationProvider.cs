// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.InheritingResxLocalizationProvider
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// A singleton RESX provider that uses inheriting attached properties.
  /// </summary>
  public class InheritingResxLocalizationProvider : ResxLocalizationProviderBase
  {
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultDictionary to set the fallback resource dictionary.
    /// </summary>
    public static readonly DependencyProperty DefaultDictionaryProperty = DependencyProperty.RegisterAttached("DefaultDictionary", typeof (string), typeof (InheritingResxLocalizationProvider), (PropertyMetadata) new FrameworkPropertyMetadata((object) null, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(InheritingResxLocalizationProvider.AttachedPropertyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultAssembly to set the fallback assembly.
    /// </summary>
    public static readonly DependencyProperty DefaultAssemblyProperty = DependencyProperty.RegisterAttached("DefaultAssembly", typeof (string), typeof (InheritingResxLocalizationProvider), (PropertyMetadata) new FrameworkPropertyMetadata((object) null, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(InheritingResxLocalizationProvider.AttachedPropertyChanged)));
    /// <summary>
    /// Lock object for the creation of the singleton instance.
    /// </summary>
    private static readonly object InstanceLock = new object();
    /// <summary>The instance of the singleton.</summary>
    private static InheritingResxLocalizationProvider _instance;

    /// <summary>
    /// Indicates, that one of the attached properties changed.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="args">The event argument.</param>
    private static void AttachedPropertyChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs args)
    {
      InheritingResxLocalizationProvider.Instance.OnProviderChanged(obj);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to get the default dictionary from.</param>
    /// <returns>The default dictionary.</returns>
    public static string GetDefaultDictionary(DependencyObject obj)
    {
      return obj.GetValueSync<string>(InheritingResxLocalizationProvider.DefaultDictionaryProperty);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to get the default assembly from.</param>
    /// <returns>The default assembly.</returns>
    public static string GetDefaultAssembly(DependencyObject obj)
    {
      return obj.GetValueSync<string>(InheritingResxLocalizationProvider.DefaultAssemblyProperty);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to set the default dictionary to.</param>
    /// <param name="value">The dictionary.</param>
    public static void SetDefaultDictionary(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(InheritingResxLocalizationProvider.DefaultDictionaryProperty, value);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to set the default assembly to.</param>
    /// <param name="value">The assembly.</param>
    public static void SetDefaultAssembly(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(InheritingResxLocalizationProvider.DefaultAssemblyProperty, value);
    }

    /// <summary>
    /// Gets the <see cref="T:WPFLocalizeExtension.Providers.ResxLocalizationProvider" /> singleton.
    /// </summary>
    public static InheritingResxLocalizationProvider Instance
    {
      get
      {
        if (InheritingResxLocalizationProvider._instance == null)
        {
          lock (InheritingResxLocalizationProvider.InstanceLock)
          {
            if (InheritingResxLocalizationProvider._instance == null)
              InheritingResxLocalizationProvider._instance = new InheritingResxLocalizationProvider();
          }
        }
        return InheritingResxLocalizationProvider._instance;
      }
    }

    /// <summary>The singleton constructor.</summary>
    private InheritingResxLocalizationProvider()
    {
      this.ResourceManagerList = new Dictionary<string, ResourceManager>();
      ObservableCollection<CultureInfo> observableCollection = new ObservableCollection<CultureInfo>();
      observableCollection.Add(CultureInfo.InvariantCulture);
      this.AvailableCultures = observableCollection;
    }

    /// <summary>Get the assembly from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The assembly name, if available.</returns>
    protected override string GetAssembly(DependencyObject target)
    {
      return target?.GetValue(InheritingResxLocalizationProvider.DefaultAssemblyProperty) as string;
    }

    /// <summary>Get the dictionary from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The dictionary name, if available.</returns>
    protected override string GetDictionary(DependencyObject target)
    {
      return target?.GetValue(InheritingResxLocalizationProvider.DefaultDictionaryProperty) as string;
    }
  }
}
