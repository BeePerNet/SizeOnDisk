// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.SafeTargetInfo
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// An extension to the <see cref="T:XAMLMarkupExtensions.Base.TargetInfo" /> class with WeakReference instead of direct object linking.
  /// </summary>
  public class SafeTargetInfo : TargetInfo
  {
    /// <summary>Gets the target object reference.</summary>
    public WeakReference TargetObjectReference { get; }

    /// <summary>Creates a new TargetInfo instance.</summary>
    /// <param name="targetObject">The target object.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="targetPropertyType">The target property type.</param>
    /// <param name="targetPropertyIndex">The target property index.</param>
    public SafeTargetInfo(
      object targetObject,
      object targetProperty,
      Type targetPropertyType,
      int targetPropertyIndex)
      : base((object) null, targetProperty, targetPropertyType, targetPropertyIndex)
    {
      this.TargetObjectReference = new WeakReference(targetObject);
    }

    /// <summary>
    /// Creates a new <see cref="T:WPFLocalizeExtension.Engine.SafeTargetInfo" /> based on a <see cref="T:XAMLMarkupExtensions.Base.TargetInfo" /> template.
    /// </summary>
    /// <param name="targetInfo">The target information.</param>
    /// <returns>A new instance with safe references.</returns>
    public static SafeTargetInfo FromTargetInfo(TargetInfo targetInfo)
    {
      return new SafeTargetInfo(targetInfo.TargetObject, targetInfo.TargetProperty, targetInfo.TargetPropertyType, targetInfo.TargetPropertyIndex);
    }
  }
}
