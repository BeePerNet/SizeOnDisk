// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ILocalizationProvider
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// An interface describing classes that provide localized values based on a source/dictionary/key combination.
  /// </summary>
  public interface ILocalizationProvider
  {
    /// <summary>
    /// Uses the key and target to build a fully qualified resource key (Assembly, Dictionary, Key)
    /// </summary>
    /// <param name="key">Key used as a base to find the full key</param>
    /// <param name="target">Target used to help determine key information</param>
    /// <returns>Returns an object with all possible pieces of the given key (Assembly, Dictionary, Key)</returns>
    FullyQualifiedResourceKeyBase GetFullyQualifiedResourceKey(
      string key,
      DependencyObject target);

    /// <summary>Get the localized object.</summary>
    /// <param name="key">The key to the value.</param>
    /// <param name="target">The target <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <param name="culture">The culture to use.</param>
    /// <returns>The value corresponding to the source/dictionary/key path for the given culture (otherwise NULL).</returns>
    object GetLocalizedObject(string key, DependencyObject target, CultureInfo culture);

    /// <summary>An observable list of available cultures.</summary>
    ObservableCollection<CultureInfo> AvailableCultures { get; }

    /// <summary>An event that is fired when the provider changed.</summary>
    event ProviderChangedEventHandler ProviderChanged;

    /// <summary>An event that is fired when an error occurred.</summary>
    event ProviderErrorEventHandler ProviderError;

    /// <summary>An event that is fired when a value changed.</summary>
    event ValueChangedEventHandler ValueChanged;
  }
}
