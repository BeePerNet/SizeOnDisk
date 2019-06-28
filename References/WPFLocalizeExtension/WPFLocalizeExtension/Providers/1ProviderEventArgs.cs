// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ProviderErrorEventArgs
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Windows;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>Events arguments for a ProviderErrorEventHandler.</summary>
  public class ProviderErrorEventArgs : EventArgs
  {
    /// <summary>The target object.</summary>
    public DependencyObject Object { get; }

    /// <summary>The key.</summary>
    public string Key { get; }

    /// <summary>The message.</summary>
    public string Message { get; }

    /// <summary>
    /// Creates a new <see cref="T:WPFLocalizeExtension.Providers.ProviderErrorEventArgs" /> instance.
    /// </summary>
    /// <param name="obj">The target object.</param>
    /// <param name="key">The key that caused the error.</param>
    /// <param name="message">The error message.</param>
    public ProviderErrorEventArgs(DependencyObject obj, string key, string message)
    {
      this.Object = obj;
      this.Key = key;
      this.Message = message;
    }
  }
}
