// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.ObjectDependencyManager
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>
  /// This class ensures, that a specific object lives as long a associated object is alive.
  /// </summary>
  public static class ObjectDependencyManager
  {
    /// <summary>
    /// This member holds the list of all <see cref="T:System.WeakReference" />s and their appropriate objects.
    /// </summary>
    private static readonly Dictionary<object, List<WeakReference>> InternalList = new Dictionary<object, List<WeakReference>>();

    /// <summary>This method adds a new object dependency</summary>
    /// <param name="weakRefDp">The <see cref="T:System.WeakReference" />, which ensures the live cycle of <paramref name="objToHold" /></param>
    /// <param name="objToHold">The object, which should stay alive as long <paramref name="weakRefDp" /> is alive</param>
    /// <returns>
    /// true, if the binding was successfully, otherwise false
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// The <paramref name="objToHold" /> cannot be null
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="objToHold" /> cannot be type of <see cref="T:System.WeakReference" />
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// The <see cref="T:System.WeakReference" />.Target cannot be the same as <paramref name="objToHold" />
    /// </exception>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static bool AddObjectDependency(WeakReference weakRefDp, object objToHold)
    {
      ObjectDependencyManager.CleanUp();
      if (objToHold == null)
        throw new ArgumentNullException(nameof (objToHold), "The objToHold cannot be null");
      if (objToHold.GetType() == typeof (WeakReference))
        throw new ArgumentException("objToHold cannot be type of WeakReference", nameof (objToHold));
      if (weakRefDp.Target == objToHold)
        throw new InvalidOperationException("The WeakReference.Target cannot be the same as objToHold");
      bool flag = false;
      if (!ObjectDependencyManager.InternalList.ContainsKey(objToHold))
      {
        List<WeakReference> weakReferenceList = new List<WeakReference>()
        {
          weakRefDp
        };
        ObjectDependencyManager.InternalList.Add(objToHold, weakReferenceList);
        flag = true;
      }
      else
      {
        List<WeakReference> weakReferenceList = ObjectDependencyManager.InternalList[objToHold];
        if (!weakReferenceList.Contains(weakRefDp))
        {
          weakReferenceList.Add(weakRefDp);
          flag = true;
        }
      }
      return flag;
    }

    /// <summary>
    /// This method cleans up all independent (!<see cref="T:System.WeakReference" />.IsAlive) objects.
    /// </summary>
    public static void CleanUp()
    {
      ObjectDependencyManager.CleanUp((object) null);
    }

    /// <summary>
    /// This method cleans up all independent (!<see cref="T:System.WeakReference" />.IsAlive) objects or a single object.
    /// </summary>
    /// <param name="objToRemove">
    /// If defined, the associated object dependency will be removed instead of a full CleanUp
    /// </param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void CleanUp(object objToRemove)
    {
      if (objToRemove != null)
      {
        if (!ObjectDependencyManager.InternalList.Remove(objToRemove))
          throw new Exception("Key was not found!");
      }
      else
      {
        List<object> objectList = new List<object>();
        foreach (KeyValuePair<object, List<WeakReference>> keyValuePair in ObjectDependencyManager.InternalList)
        {
          for (int index = keyValuePair.Value.Count - 1; index >= 0; --index)
          {
            if (keyValuePair.Value[index].Target == null)
              keyValuePair.Value.RemoveAt(index);
          }
          if (keyValuePair.Value.Count == 0)
            objectList.Add(keyValuePair.Key);
        }
        for (int index = objectList.Count - 1; index >= 0; --index)
          ObjectDependencyManager.InternalList.Remove(objectList[index]);
        objectList.Clear();
      }
    }
  }
}
