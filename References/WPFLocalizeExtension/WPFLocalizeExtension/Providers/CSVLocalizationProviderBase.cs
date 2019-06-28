// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.CSVLocalizationProviderBase
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>The base for CSV file providers.</summary>
  public abstract class CSVLocalizationProviderBase : DependencyObject, ILocalizationProvider
  {
    /// <summary>
    /// Lock object for concurrent access to the resource manager list.
    /// </summary>
    protected object ResourceManagerListLock = new object();
    /// <summary>
    /// Lock object for concurrent access to the available culture list.
    /// </summary>
    protected object AvailableCultureListLock = new object();
    /// <summary>Holds the name of the Resource Manager.</summary>
    private const string ResourceManagerName = "ResourceManager";
    /// <summary>Holds the extension of the resource files.</summary>
    private const string ResourceFileExtension = ".resources";
    /// <summary>
    /// Holds the binding flags for the reflection to find the resource files.
    /// </summary>
    private const BindingFlags ResourceBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary>
    /// Gets the used ResourceManagers with their corresponding <c>namespaces</c>.
    /// </summary>
    protected Dictionary<string, ResourceManager> ResourceManagerList;

    /// <summary>
    /// Returns the <see cref="T:System.Reflection.AssemblyName" /> of the passed assembly instance
    /// </summary>
    /// <param name="assembly">The Assembly where to get the name from</param>
    /// <returns>The Assembly name</returns>
    protected string GetAssemblyName(Assembly assembly)
    {
      if (assembly == (Assembly) null)
        throw new ArgumentNullException(nameof (assembly));
      if (assembly.FullName == null)
        throw new NullReferenceException("assembly.FullName is null");
      return assembly.FullName.Split(',')[0];
    }

    /// <summary>
    /// Parses a key ([[Assembly:]Dict:]Key and return the parts of it.
    /// </summary>
    /// <param name="inKey">The key to parse.</param>
    /// <param name="outAssembly">The found or default assembly.</param>
    /// <param name="outDict">The found or default dictionary.</param>
    /// <param name="outKey">The found or default key.</param>
    public static void ParseKey(
      string inKey,
      out string outAssembly,
      out string outDict,
      out string outKey)
    {
      outAssembly = (string) null;
      outDict = (string) null;
      outKey = (string) null;
      if (string.IsNullOrEmpty(inKey))
        return;
      string[] strArray = inKey.Trim().Split(":".ToCharArray());
      if (strArray.Length == 3)
      {
        outAssembly = !string.IsNullOrEmpty(strArray[0]) ? strArray[0] : (string) null;
        outDict = !string.IsNullOrEmpty(strArray[1]) ? strArray[1] : (string) null;
        outKey = strArray[2];
      }
      if (strArray.Length == 2)
      {
        outDict = !string.IsNullOrEmpty(strArray[0]) ? strArray[0] : (string) null;
        outKey = strArray[1];
      }
      if (strArray.Length != 1)
        return;
      outKey = strArray[0];
    }

    /// <summary>Get the assembly from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The assembly name, if available.</returns>
    protected abstract string GetAssembly(DependencyObject target);

    /// <summary>Get the dictionary from the context, if possible.</summary>
    /// <param name="target">The target object.</param>
    /// <returns>The dictionary name, if available.</returns>
    protected abstract string GetDictionary(DependencyObject target);

    /// <summary>Thread-safe access to the AvailableCultures list.</summary>
    /// <param name="c">The CultureInfo.</param>
    protected void AddCulture(CultureInfo c)
    {
      lock (this.AvailableCultureListLock)
      {
        if (this.AvailableCultures.Contains(c))
          return;
        this.AvailableCultures.Add(c);
      }
    }

    /// <summary>
    /// Uses the key and target to build a fully qualified resource key (Assembly, Dictionary, Key)
    /// </summary>
    /// <param name="key">Key used as a base to find the full key</param>
    /// <param name="target">Target used to help determine key information</param>
    /// <returns>Returns an object with all possible pieces of the given key (Assembly, Dictionary, Key)</returns>
    public FullyQualifiedResourceKeyBase GetFullyQualifiedResourceKey(
      string key,
      DependencyObject target)
    {
      if (string.IsNullOrEmpty(key))
        return (FullyQualifiedResourceKeyBase) null;
      string outAssembly;
      string outDict;
      CSVLocalizationProviderBase.ParseKey(key, out outAssembly, out outDict, out key);
      if (target == null)
        return (FullyQualifiedResourceKeyBase) new FQAssemblyDictionaryKey(key, outAssembly, outDict);
      if (string.IsNullOrEmpty(outAssembly))
        outAssembly = this.GetAssembly(target);
      if (string.IsNullOrEmpty(outDict))
        outDict = this.GetDictionary(target);
      return (FullyQualifiedResourceKeyBase) new FQAssemblyDictionaryKey(key, outAssembly, outDict);
    }

    /// <summary>Gets fired when the provider changed.</summary>
    public event ProviderChangedEventHandler ProviderChanged;

    /// <summary>An event that is fired when an error occurred.</summary>
    public event ProviderErrorEventHandler ProviderError;

    /// <summary>An event that is fired when a value changed.</summary>
    public event ValueChangedEventHandler ValueChanged;

    /// <summary>
    /// Calls the <see cref="E:WPFLocalizeExtension.Providers.ILocalizationProvider.ProviderChanged" /> event.
    /// </summary>
    /// <param name="target">The target object.</param>
    protected virtual void OnProviderChanged(DependencyObject target)
    {
      try
      {
        this.GetAssembly(target);
        this.GetDictionary(target);
      }
      catch
      {
      }
      ProviderChangedEventHandler providerChanged = this.ProviderChanged;
      if (providerChanged == null)
        return;
      providerChanged((object) this, new ProviderChangedEventArgs(target));
    }

    /// <summary>
    /// Calls the <see cref="E:WPFLocalizeExtension.Providers.ILocalizationProvider.ProviderError" /> event.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="key">The key.</param>
    /// <param name="message">The error message.</param>
    protected virtual void OnProviderError(DependencyObject target, string key, string message)
    {
      ProviderErrorEventHandler providerError = this.ProviderError;
      if (providerError == null)
        return;
      providerError((object) this, new ProviderErrorEventArgs(target, key, message));
    }

    /// <summary>
    /// Calls the <see cref="E:WPFLocalizeExtension.Providers.ILocalizationProvider.ValueChanged" /> event.
    /// </summary>
    /// <param name="key">The key where the value was changed.</param>
    /// <param name="value">The new value.</param>
    /// <param name="tag">A custom tag.</param>
    protected virtual void OnValueChanged(string key, object value, object tag)
    {
      ValueChangedEventHandler valueChanged = this.ValueChanged;
      if (valueChanged == null)
        return;
      valueChanged((object) this, new ValueChangedEventArgs(key, value, tag));
    }

    /// <summary>Get the localized object.</summary>
    /// <param name="key">The key to the value.</param>
    /// <param name="target">The target object.</param>
    /// <param name="culture">The culture to use.</param>
    /// <returns>The value corresponding to the source/dictionary/key path for the given culture (otherwise NULL).</returns>
    public virtual object GetLocalizedObject(
      string key,
      DependencyObject target,
      CultureInfo culture)
    {
      throw new InvalidOperationException("GetLocalizedObject needs to be overriden");
    }

    /// <summary>An observable list of available cultures.</summary>
    public ObservableCollection<CultureInfo> AvailableCultures { get; protected set; }
  }
}
