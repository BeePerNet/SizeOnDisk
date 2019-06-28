using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public class CommandEx: RoutedUICommand
    {



































        public static readonly DependencyProperty ContextProperty = DependencyProperty.RegisterAttached("Context",
          typeof(ICommandContext), typeof(CommandEx),
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





        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(CommandEx), new PropertyMetadata(_CommandPropertyChanged));
        private static void _CommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            /*MenuItem target = d as MenuItem;
            if (target != null)
            {
                IMenuCommand command = (IMenuCommand)e.NewValue;
                if (command != null)
                {
                    target.Command = command;
                    target.Header = command.Header;
                }
                else
                {
                    target.Command = null;
                    target.Header = null;
                }
            }*/
        }
        public static void SetCommand(Control target, CommandEx command)
        {
            target.SetValue(CommandProperty, command);
        }
        public static CommandEx GetCommand(Control target)
        {
            return (CommandEx)target.GetValue(CommandProperty);
        }










    }
}
