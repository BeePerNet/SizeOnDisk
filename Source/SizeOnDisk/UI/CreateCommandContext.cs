using System;
using System.Windows;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public static class CreateCommandContext
    {
        public static readonly DependencyProperty ContextProperty
          = DependencyProperty.RegisterAttached("Context",
              typeof(ICommandContext), typeof(CreateCommandContext),
              new PropertyMetadata(new PropertyChangedCallback(OnCommandInvalidated)));

        public static ICommandContext GetContext(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element", "is null");
            return element.GetValue(ContextProperty) as ICommandContext;
        }

        public static void SetContext(DependencyObject element, ICommandContext commandContext)
        {
            if (element == null)
                throw new ArgumentNullException("element", "is null");
            element.SetValue(ContextProperty, commandContext);
        }

        /// <summary>  
        /// Callback when the Command property is set or changed.  
        /// </summary>  
        private static void OnCommandInvalidated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            // Clear the exisiting bindings on the element we are attached to.  
            UIElement element = (UIElement)dependencyObject;
            element.CommandBindings.Clear();

            // If we're given a command model, set up a binding  
            ICommandContext commandContext = e.NewValue as ICommandContext;
            if (commandContext != null)
            {
                foreach (CommandBinding commandBinding in commandContext.Commands)
                {
                    element.CommandBindings.Add(commandBinding);
                }
            }

            if (commandContext != null)
            {
                foreach (InputBinding inputBinding in commandContext.Inputs)
                {
                    element.InputBindings.Add(inputBinding);
                }
            }

            // Suggest to WPF to refresh commands  
            CommandManager.InvalidateRequerySuggested();
        }

    }
}
