﻿#region Copyright information
// <copyright file="ParentChangedNotifierHelper.cs">
//     Licensed under Microsoft Public License (Ms-PL)
//     http://wpflocalizeextension.codeplex.com/license
// </copyright>
// <author>Bernhard Millauer</author>
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using XAMLMarkupExtensions.Base;

namespace WPFLocalizeExtension.Engine
{
    /// <summary>
    /// A memory safe dictionary storage for <see cref="ParentChangedNotifier"/> instances.
    /// </summary>
	public class ParentNotifiers
    {
        readonly Dictionary<WeakReference<DependencyObject>, WeakReference<ParentChangedNotifier>> _inner =
            new Dictionary<WeakReference<DependencyObject>, WeakReference<ParentChangedNotifier>>();

        /// <summary>
        /// Check, if it contains the key.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <returns>True, if the key exists.</returns>
        public bool ContainsKey(DependencyObject target)
        {
            return _inner.Keys.Any(x => x.TryGetTarget(out DependencyObject internaltarget) && ReferenceEquals(internaltarget, target));
        }

        /// <summary>
        /// Removes the entry.
        /// </summary>
        /// <param name="target">The target object.</param>
		public void Remove(DependencyObject target)
        {
            WeakReference<DependencyObject> singleOrDefault =
                _inner.Keys.SingleOrDefault(x => x.TryGetTarget(out DependencyObject internaltarget) && ReferenceEquals(internaltarget, target));

            if (singleOrDefault != null)
            {
                //if (_inner[singleOrDefault].IsAlive)
                  //  _inner[singleOrDefault].Target.Dispose();
                _inner.Remove(singleOrDefault);
            }
        }

        /// <summary>
        /// Adds the key-value-pair.
        /// </summary>
        /// <param name="target">The target key object.</param>
        /// <param name="parentChangedNotifier">The notifier.</param>
		public void Add(DependencyObject target, ParentChangedNotifier parentChangedNotifier)
        {
            _inner.Add(new WeakReference<DependencyObject>(target), new WeakReference<ParentChangedNotifier>(parentChangedNotifier));
        }
    }
}