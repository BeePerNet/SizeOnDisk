// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.CSVEmbeddedLocalizationProvider
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows;
using WPFLocalizeExtension.Engine;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// A singleton CSV provider that uses attached properties and the Parent property to iterate through the visual tree.
  /// </summary>
  public class CSVEmbeddedLocalizationProvider : CSVLocalizationProviderBase
  {
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultDictionary to set the fallback resource dictionary.
    /// </summary>
    public static readonly DependencyProperty DefaultDictionaryProperty = DependencyProperty.RegisterAttached("DefaultDictionary", typeof (string), typeof (CSVEmbeddedLocalizationProvider), new PropertyMetadata((object) null, new PropertyChangedCallback(CSVEmbeddedLocalizationProvider.AttachedPropertyChanged)));
    /// <summary>
    /// <see cref="T:System.Windows.DependencyProperty" /> DefaultAssembly to set the fallback assembly.
    /// </summary>
    public static readonly DependencyProperty DefaultAssemblyProperty = DependencyProperty.RegisterAttached("DefaultAssembly", typeof (string), typeof (CSVEmbeddedLocalizationProvider), new PropertyMetadata((object) null, new PropertyChangedCallback(CSVEmbeddedLocalizationProvider.AttachedPropertyChanged)));
    /// <summary>
    /// Lock object for the creation of the singleton instance.
    /// </summary>
    private static readonly object InstanceLock = new object();
    /// <summary>
    /// A dictionary for notification classes for changes of the individual target Parent changes.
    /// </summary>
    private readonly ParentNotifiers _parentNotifiers = new ParentNotifiers();
    /// <summary>The instance of the singleton.</summary>
    private static CSVEmbeddedLocalizationProvider _instance;
    private bool _hasHeader;

    /// <summary>
    /// Indicates, that one of the attached properties changed.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="args">The event argument.</param>
    private static void AttachedPropertyChanged(
      DependencyObject obj,
      DependencyPropertyChangedEventArgs args)
    {
      CSVEmbeddedLocalizationProvider.Instance.OnProviderChanged(obj);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to get the default dictionary from.</param>
    /// <returns>The default dictionary.</returns>
    public static string GetDefaultDictionary(DependencyObject obj)
    {
      return obj.GetValueSync<string>(CSVEmbeddedLocalizationProvider.DefaultDictionaryProperty);
    }

    /// <summary>
    /// Getter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to get the default assembly from.</param>
    /// <returns>The default assembly.</returns>
    public static string GetDefaultAssembly(DependencyObject obj)
    {
      return obj.GetValueSync<string>(CSVEmbeddedLocalizationProvider.DefaultAssemblyProperty);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default dictionary.
    /// </summary>
    /// <param name="obj">The dependency object to set the default dictionary to.</param>
    /// <param name="value">The dictionary.</param>
    public static void SetDefaultDictionary(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(CSVEmbeddedLocalizationProvider.DefaultDictionaryProperty, value);
    }

    /// <summary>
    /// Setter of <see cref="T:System.Windows.DependencyProperty" /> default assembly.
    /// </summary>
    /// <param name="obj">The dependency object to set the default assembly to.</param>
    /// <param name="value">The assembly.</param>
    public static void SetDefaultAssembly(DependencyObject obj, string value)
    {
      obj.SetValueSync<string>(CSVEmbeddedLocalizationProvider.DefaultAssemblyProperty, value);
    }

    /// <summary>
    /// Gets the <see cref="T:WPFLocalizeExtension.Providers.CSVEmbeddedLocalizationProvider" /> singleton.
    /// </summary>
    public static CSVEmbeddedLocalizationProvider Instance
    {
      get
      {
        if (CSVEmbeddedLocalizationProvider._instance == null)
        {
          lock (CSVEmbeddedLocalizationProvider.InstanceLock)
          {
            if (CSVEmbeddedLocalizationProvider._instance == null)
              CSVEmbeddedLocalizationProvider._instance = new CSVEmbeddedLocalizationProvider();
          }
        }
        return CSVEmbeddedLocalizationProvider._instance;
      }
    }

    /// <summary>The singleton constructor.</summary>
    private CSVEmbeddedLocalizationProvider()
    {
      this.ResourceManagerList = new Dictionary<string, ResourceManager>();
      ObservableCollection<CultureInfo> observableCollection = new ObservableCollection<CultureInfo>();
      observableCollection.Add(CultureInfo.InvariantCulture);
      this.AvailableCultures = observableCollection;
    }

    /// <summary>A flag indicating, if it has a header row.</summary>
    public bool HasHeader
    {
      get
      {
        return this._hasHeader;
      }
      set
      {
        this._hasHeader = value;
      }
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
        return (string) null;
      return target.GetValueOrRegisterParentNotifier<string>(CSVEmbeddedLocalizationProvider.DefaultAssemblyProperty, new Action<DependencyObject>(this.ParentChangedAction), this._parentNotifiers);
    }

    /// <summary>Get the dictionary from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The dictionary name, if available.</returns>
    protected override string GetDictionary(DependencyObject target)
    {
      if (target == null)
        return (string) null;
      return target.GetValueOrRegisterParentNotifier<string>(CSVEmbeddedLocalizationProvider.DefaultDictionaryProperty, new Action<DependencyObject>(this.ParentChangedAction), this._parentNotifiers);
    }

    /// <summary>Get the localized object.</summary>
    /// <param name="key">The key to the value.</param>
    /// <param name="target">The target object.</param>
    /// <param name="culture">The culture to use.</param>
    /// <returns>The value corresponding to the source/dictionary/key path for the given culture (otherwise NULL).</returns>
    public override object GetLocalizedObject(
      string key,
      DependencyObject target,
      CultureInfo culture)
    {
      string str1 = (string) null;
      string name = "";
      string outAssembly;
      string dictionary;
      CSVLocalizationProviderBase.ParseKey(key, out outAssembly, out dictionary, out key);
      if (string.IsNullOrEmpty(outAssembly))
        outAssembly = this.GetAssembly(target);
      if (string.IsNullOrEmpty(dictionary))
        dictionary = this.GetDictionary(target);
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (new AssemblyName(assembly.FullName).Name == outAssembly)
        {
          name = ((IEnumerable<string>) assembly.GetManifestResourceNames()).FirstOrDefault<string>((Func<string, bool>) (r => r.Contains(dictionary + (string.IsNullOrEmpty(culture.Name) ? "" : "-") + culture.Name)));
          if (name != null)
          {
            Stream manifestResourceStream = assembly.GetManifestResourceStream(name);
            if (manifestResourceStream == null)
              throw new InvalidOperationException();
            using (StreamReader streamReader = new StreamReader(manifestResourceStream, Encoding.Default))
            {
              if (this.HasHeader && !streamReader.EndOfStream)
                streamReader.ReadLine();
              while (!streamReader.EndOfStream)
              {
                string str2 = streamReader.ReadLine();
                if (str2 != null)
                {
                  string[] strArray = str2.Split(";".ToCharArray());
                  if (strArray.Length >= 2 && !(strArray[0] != key))
                  {
                    str1 = strArray[1];
                    break;
                  }
                }
                else
                  break;
              }
            }
          }
        }
      }
      if (str1 == null)
        this.OnProviderError(target, key, "The key does not exist in " + name + ".");
      return (object) str1;
    }
  }
}
