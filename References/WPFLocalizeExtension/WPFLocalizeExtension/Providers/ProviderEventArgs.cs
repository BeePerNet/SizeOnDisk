// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ProviderChangedEventArgs
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Windows;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>Events arguments for a ProviderChangedEventHandler.</summary>
  public class ProviderChangedEventArgs : EventArgs
  {
    /// <summary>The target object.</summary>
    public DependencyObject Object { get; }

    /// <summary>
    /// Creates a new <see cref="T:WPFLocalizeExtension.Providers.ProviderChangedEventArgs" /> instance.
    /// </summary>
    /// <param name="obj">The target object.</param>
    public ProviderChangedEventArgs(DependencyObject obj)
    {
      this.Object = obj;
    }
  }
}
