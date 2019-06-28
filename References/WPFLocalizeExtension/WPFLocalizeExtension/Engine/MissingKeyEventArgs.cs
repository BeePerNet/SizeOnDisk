// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.MissingKeyEventArgs
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>Event arguments for a missing key event.</summary>
  public class MissingKeyEventArgs : EventArgs
  {
    /// <summary>The key that is missing or has no data.</summary>
    public string Key { get; }

    /// <summary>A flag indicating that a reload should be performed.</summary>
    public bool Reload { get; set; }

    /// <summary>A custom returnmessage for the missing key</summary>
    public string MissingKeyResult { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="T:WPFLocalizeExtension.Engine.MissingKeyEventArgs" />.
    /// </summary>
    /// <param name="key">The missing key.</param>
    public MissingKeyEventArgs(string key)
    {
      this.Key = key;
      this.Reload = false;
      this.MissingKeyResult = (string) null;
    }
  }
}
