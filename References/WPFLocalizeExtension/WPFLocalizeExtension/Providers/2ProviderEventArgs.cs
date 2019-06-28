// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ValueChangedEventArgs
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>Events arguments for a ValueChangedEventHandler.</summary>
  public class ValueChangedEventArgs : EventArgs
  {
    /// <summary>A custom tag.</summary>
    public object Tag { get; }

    /// <summary>The new value.</summary>
    public object Value { get; }

    /// <summary>The key.</summary>
    public string Key { get; }

    /// <summary>
    /// Creates a new <see cref="T:WPFLocalizeExtension.Providers.ValueChangedEventArgs" /> instance.
    /// </summary>
    /// <param name="key">The key where the value was changed.</param>
    /// <param name="value">The new value.</param>
    /// <param name="tag">A custom tag.</param>
    public ValueChangedEventArgs(string key, object value, object tag)
    {
      this.Key = key;
      this.Value = value;
      this.Tag = tag;
    }
  }
}
