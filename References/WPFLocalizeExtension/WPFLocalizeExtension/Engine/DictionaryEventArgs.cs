// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.DictionaryEventArgs
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>Event argument for dictionary events.</summary>
  public class DictionaryEventArgs : EventArgs
  {
    /// <summary>The type of the event.</summary>
    public DictionaryEventType Type { get; }

    /// <summary>A corresponding tag.</summary>
    public object Tag { get; }

    /// <summary>The constructor.</summary>
    /// <param name="type">The type of the event.</param>
    /// <param name="tag">The corresponding tag.</param>
    public DictionaryEventArgs(DictionaryEventType type, object tag)
    {
      this.Type = type;
      this.Tag = tag;
    }

    /// <summary>Returns the type and tag as a string.</summary>
    /// <returns>The type and tag as a string.</returns>
    public override string ToString()
    {
      return ((int) this.Type).ToString() + ": " + this.Tag;
    }
  }
}
