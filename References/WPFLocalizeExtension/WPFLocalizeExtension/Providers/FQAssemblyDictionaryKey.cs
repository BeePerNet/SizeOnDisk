// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.FQAssemblyDictionaryKey
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// A class that bundles the key, assembly and dictionary information.
  /// </summary>
  public class FQAssemblyDictionaryKey : FullyQualifiedResourceKeyBase
  {
    private readonly string _key;
    private readonly string _assembly;
    private readonly string _dictionary;

    /// <summary>The key.</summary>
    public string Key
    {
      get
      {
        return this._key;
      }
    }

    /// <summary>The assembly of the dictionary.</summary>
    public string Assembly
    {
      get
      {
        return this._assembly;
      }
    }

    /// <summary>The resource dictionary.</summary>
    public string Dictionary
    {
      get
      {
        return this._dictionary;
      }
    }

    /// <summary>
    /// Creates a new instance of <see cref="T:WPFLocalizeExtension.Providers.FullyQualifiedResourceKeyBase" />.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="assembly">The assembly of the dictionary.</param>
    /// <param name="dictionary">The resource dictionary.</param>
    public FQAssemblyDictionaryKey(string key, string assembly = null, string dictionary = null)
    {
      this._key = key;
      this._assembly = assembly;
      this._dictionary = dictionary;
    }

    /// <summary>Converts the object to a string.</summary>
    /// <returns>The joined version of the assembly, dictionary and key.</returns>
    public override string ToString()
    {
      return string.Join(":", ((IEnumerable<string>) new string[3]
      {
        this.Assembly,
        this.Dictionary,
        this.Key
      }).Where<string>((Func<string, bool>) (x => !string.IsNullOrEmpty(x))).ToArray<string>());
    }
  }
}
