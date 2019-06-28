// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.DictionaryEventType
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

namespace WPFLocalizeExtension.Engine
{
  /// <summary>An enumeration of dictionary event types.</summary>
  public enum DictionaryEventType
  {
    /// <summary>The separation changed.</summary>
    SeparationChanged,
    /// <summary>The provider changed.</summary>
    ProviderChanged,
    /// <summary>A provider reports an update.</summary>
    ProviderUpdated,
    /// <summary>The culture changed.</summary>
    CultureChanged,
    /// <summary>A certain value changed.</summary>
    ValueChanged,
  }
}
