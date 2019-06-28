// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.IDictionaryEventListener
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System.Windows;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// Interface for listeners on dictionary events of the <see cref="T:WPFLocalizeExtension.Engine.LocalizeDictionary" /> class.
  /// </summary>
  public interface IDictionaryEventListener
  {
    /// <summary>
    /// This method is called when the resource somehow changed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    void ResourceChanged(DependencyObject sender, DictionaryEventArgs e);
  }
}
