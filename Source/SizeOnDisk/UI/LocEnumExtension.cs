using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using WPFLocalizeExtension.BaseExtensions;
using WPFLocalizeExtension.Engine;

namespace SizeOnDisk.UI
{
    /// <summary>
    /// Add any [enum key] (item name) to the resource (resx) for the enum to translate (Ex: UISizeFormatType)
    /// Add item for each enum value to the resource with format [enum key]_[enum value name] (UISizeFormatType_Bytes)
    /// Add the LocEnumExtension xaml tag to source or datacontext property of a listing control
    /// Provide path to the [enum key], and the enum type to list
    /// </summary>
    /// <example>
    /// <code><ComboBox ItemsSource="{ui:LocEnum SizeOnDisk:Localization:UISizeFormatType, {x:Type conv:UISizeFormatType}}" /></code>
    /// </example>
    [MarkupExtensionReturnType(typeof(ReadOnlyObservableCollection<EnumValue>))]
    public class LocEnumExtension : BaseLocalizeExtension<ReadOnlyObservableCollection<EnumValue>>
    {
        private Type _EnumType;

        public LocEnumExtension()
        {
        }

        /// <param name="key">Path to the [enum key] used in the resource file.</param>
        /// <param name="enumType">Enum type to list and translate.</param>
        public LocEnumExtension(string key, Type enumType)
            : base(key)
        {
            _EnumType = enumType;
            _ReadOnlyValues = new ReadOnlyObservableCollection<EnumValue>(_Values);
        }

        private ObservableCollection<EnumValue> _Values = new ObservableCollection<EnumValue>();
        private ReadOnlyObservableCollection<EnumValue> _ReadOnlyValues;

        public ReadOnlyCollection<EnumValue> Values
        {
            get
            {
                return _ReadOnlyValues;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider", "serviceProvider is null");

            base.ProvideValue(serviceProvider);

            IProvideValueTarget service = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            Selector target = service.TargetObject as Selector;
            if (target == null)
                throw new ArgumentException("TargetObject is not ItemsControl");

            Array values = Enum.GetValues(_EnumType);
            foreach (object value in values)
            {
                _Values.Add(new EnumValue((Enum)value,
                    LocalizeDictionary.Instance.GetLocalizedObject<string>(this.Assembly, this.Dict, string.Concat(this.Key, '_', value.ToString()), this.GetForcedCultureOrDefault())));
            }
            target.DisplayMemberPath = "Text";
            target.SelectedValuePath = "Value";
            return _ReadOnlyValues;
        }

        protected override void HandleNewValue()
        {
            foreach (EnumValue value in _ReadOnlyValues)
            {
                value.Text = LocalizeDictionary.Instance.GetLocalizedObject<string>(this.Assembly, this.Dict, string.Concat(this.Key, '_', value.Value.ToString()), this.GetForcedCultureOrDefault());
            }
        }

        protected override object FormatOutput(object input)
        {
            return null;
        }

    }

    public class EnumValue : INotifyPropertyChanged
    {
        internal EnumValue(Enum value, string text)
        {
            _Value = value;
            _Text = text;
        }

        private string _Text;

        public string Text
        {
            get { return _Text; }
            internal set
            {
                if (_Text != value)
                {
                    _Text = value;
                    this.OnTextChanged();
                }
            }
        }
        private Enum _Value;

        public Enum Value
        {
            get { return _Value; }
        }

        protected void OnTextChanged()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Text"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
