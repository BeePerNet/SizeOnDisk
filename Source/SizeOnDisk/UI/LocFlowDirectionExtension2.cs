using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using WPFLocalizeExtension.BaseExtensions;
using WPFLocalizeExtension.Engine;

namespace SizeOnDisk.UI
{
    /// <summary>
    /// Add FlowDirection tag to the Window xaml implementation
    /// </summary>
    /// <example>
    /// <code>FlowDirection="{ui:LocFlowDirectionExtension2}"</code>
    /// </example>
    [MarkupExtensionReturnType(typeof(FlowDirection))]
    public class LocFlowDirectionExtension2 : BaseLocalizeExtension<FlowDirection>
    {
        public LocFlowDirectionExtension2()
            : base("::LocFlowDirectionExtension2::")
        {

        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return this;
            }
            IProvideValueTarget service = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (service == null)
            {
                return this;
            }
            if (service.TargetObject is Binding)
            {
                throw new InvalidOperationException("Use as binding is not supported!");
            }
            object targetProperty = null;
            if ((service.TargetProperty is DependencyProperty) || (service.TargetProperty is PropertyInfo))
            {
                targetProperty = service.TargetProperty;
            }
            if (targetProperty == null)
            {
                return this;
            }
            if (!((service.TargetObject is DependencyObject) || (service.TargetProperty is PropertyInfo)))
            {
                return this;
            }
            bool flag = false;
            foreach (KeyValuePair<WeakReference, object> pair in this.TargetObjects)
            {
                if ((pair.Key.Target == service.TargetObject) && (pair.Value == service.TargetProperty))
                {
                    flag = true;
                    break;
                }
            }
            if ((service.TargetObject is DependencyObject) && !flag)
            {
                if (this.TargetObjects.Count == 0)
                {
                    LocalizeDictionary.Instance.AddEventListener(this);
                }
                this.TargetObjects.Add(new WeakReference(service.TargetObject), service.TargetProperty);
                ObjectDependencyManager.AddObjectDependency(new WeakReference(service.TargetObject), this);
            }
            return LocalizeDictionary.Instance.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        protected override void HandleNewValue()
        {
            this.SetNewValue(LocalizeDictionary.Instance.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        }

        protected override object FormatOutput(object input)
        {
            return input;
        }
    }
}
