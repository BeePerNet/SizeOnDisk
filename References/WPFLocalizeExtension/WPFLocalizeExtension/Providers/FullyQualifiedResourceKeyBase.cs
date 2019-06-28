// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.FullyQualifiedResourceKeyBase
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

namespace WPFLocalizeExtension.Providers
{
  /// <summary>An abstract class for key identification.</summary>
  public abstract class FullyQualifiedResourceKeyBase
  {
    /// <summary>Implicit string operator.</summary>
    /// <param name="fullyQualifiedResourceKey">The object.</param>
    /// <returns>The joined version of the assembly, dictionary and key.</returns>
    public static implicit operator string(
      FullyQualifiedResourceKeyBase fullyQualifiedResourceKey)
    {
      return fullyQualifiedResourceKey?.ToString();
    }
  }
}
