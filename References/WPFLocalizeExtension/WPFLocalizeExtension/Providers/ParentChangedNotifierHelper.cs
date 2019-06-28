// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Providers.ParentChangedNotifierHelper
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WPFLocalizeExtension.Engine;
using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Providers
{
  /// <summary>
  /// Extension methods for <see cref="T:System.Windows.DependencyObject" /> in conjunction with the <see cref="T:XAMLMarkupExtensions.Base.ParentChangedNotifier" />.
  /// </summary>
  public static class ParentChangedNotifierHelper
  {
    /// <summary>
    /// Tries to get a value that is stored somewhere in the visual tree above this <see cref="T:System.Windows.DependencyObject" />.
    /// <para>If this is not available, it will register a <see cref="T:XAMLMarkupExtensions.Base.ParentChangedNotifier" /> on the last element.</para>
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="target">The <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <param name="getFunction">The function that gets the value from a <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <param name="parentChangedAction">The notification action on the change event of the Parent property.</param>
    /// <param name="parentNotifiers">A dictionary of already registered notifiers.</param>
    /// <returns>The value, if possible.</returns>
    public static T GetValueOrRegisterParentNotifier<T>(
      this DependencyObject target,
      Func<DependencyObject, T> getFunction,
      Action<DependencyObject> parentChangedAction,
      ParentNotifiers parentNotifiers)
    {
      T obj = default (T);
      if (target == null)
        return obj;
      DependencyObject depObj = target;
      WeakReference weakTarget = new WeakReference((object) target);
      while ((object) obj == null)
      {
        obj = getFunction(depObj);
        if ((object) obj != null)
          parentNotifiers.Remove(target);
        if (!(depObj is ToolTip) && (depObj is Visual || depObj is Visual3D || depObj is FrameworkContentElement) && !(depObj is Window))
        {
          DependencyObject dependencyObject;
          if (depObj is FrameworkContentElement frameworkContentElement)
          {
            dependencyObject = frameworkContentElement.Parent;
          }
          else
          {
            try
            {
              dependencyObject = depObj.GetParent(false);
            }
            catch
            {
              dependencyObject = (DependencyObject) null;
            }
          }
          if (dependencyObject == null)
          {
            try
            {
              dependencyObject = depObj.GetParent(true);
            }
            catch
            {
              break;
            }
          }
          if (dependencyObject == null && depObj is FrameworkElement)
            dependencyObject = ((FrameworkElement) depObj).Parent;
          if ((object) obj == null && dependencyObject == null)
          {
            if (depObj is FrameworkElement element && !parentNotifiers.ContainsKey(target))
            {
              Action onParentChanged = (Action) (() =>
              {
                DependencyObject target1 = (DependencyObject) weakTarget.Target;
                if (target1 == null)
                  return;
                parentChangedAction(target1);
                parentNotifiers.Remove(target1);
              });
              ParentChangedNotifier parentChangedNotifier = new ParentChangedNotifier(element, onParentChanged);
              parentNotifiers.Add(target, parentChangedNotifier);
              break;
            }
            break;
          }
          depObj = dependencyObject;
        }
        else
          break;
      }
      return obj;
    }

    /// <summary>
    /// Tries to get a value that is stored somewhere in the visual tree above this <see cref="T:System.Windows.DependencyObject" />.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="target">The <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <param name="getFunction">The function that gets the value from a <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <returns>The value, if possible.</returns>
    public static T GetValue<T>(this DependencyObject target, Func<DependencyObject, T> getFunction)
    {
      T obj = default (T);
      if (target != null)
      {
        DependencyObject depObj = target;
        while ((object) obj == null)
        {
          obj = getFunction(depObj);
          if (depObj is Visual || depObj is Visual3D || depObj is FrameworkContentElement)
          {
            DependencyObject parent;
            if (depObj is FrameworkContentElement frameworkContentElement)
            {
              parent = frameworkContentElement.Parent;
            }
            else
            {
              try
              {
                parent = depObj.GetParent(true);
              }
              catch
              {
                break;
              }
            }
            if (parent == null && depObj is FrameworkElement)
              parent = ((FrameworkElement) depObj).Parent;
            if ((object) obj != null || parent != null)
              depObj = parent;
            else
              break;
          }
          else
            break;
        }
      }
      return obj;
    }

    /// <summary>
    /// Tries to get a value from a <see cref="T:System.Windows.DependencyProperty" /> that is stored somewhere in the visual tree above this <see cref="T:System.Windows.DependencyObject" />.
    /// If this is not available, it will register a <see cref="T:XAMLMarkupExtensions.Base.ParentChangedNotifier" /> on the last element.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="target">The <see cref="T:System.Windows.DependencyObject" />.</param>
    /// <param name="property">A <see cref="T:System.Windows.DependencyProperty" /> that will be read out.</param>
    /// <param name="parentChangedAction">The notification action on the change event of the Parent property.</param>
    /// <param name="parentNotifiers">A dictionary of already registered notifiers.</param>
    /// <returns>The value, if possible.</returns>
    public static T GetValueOrRegisterParentNotifier<T>(
      this DependencyObject target,
      DependencyProperty property,
      Action<DependencyObject> parentChangedAction,
      ParentNotifiers parentNotifiers)
    {
      return target.GetValueOrRegisterParentNotifier<T>((Func<DependencyObject, T>) (depObj => depObj.GetValueSync<T>(property)), parentChangedAction, parentNotifiers);
    }

    /// <summary>Gets the parent in the visual or logical tree.</summary>
    /// <param name="depObj">The dependency object.</param>
    /// <param name="isVisualTree">True for visual tree, false for logical tree.</param>
    /// <returns>The parent, if available.</returns>
    public static DependencyObject GetParent(
      this DependencyObject depObj,
      bool isVisualTree)
    {
      if (depObj.CheckAccess())
        return ParentChangedNotifierHelper.GetParentInternal(depObj, isVisualTree);
      return depObj.Dispatcher.Invoke<DependencyObject>((Func<DependencyObject>) (() => ParentChangedNotifierHelper.GetParentInternal(depObj, isVisualTree)));
    }

    private static DependencyObject GetParentInternal(
      DependencyObject depObj,
      bool isVisualTree)
    {
      if (isVisualTree)
        return VisualTreeHelper.GetParent(depObj);
      return LogicalTreeHelper.GetParent(depObj);
    }
  }
}
