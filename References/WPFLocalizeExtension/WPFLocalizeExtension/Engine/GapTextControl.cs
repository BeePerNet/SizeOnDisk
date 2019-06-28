// Decompiled with JetBrains decompiler
// Type: WPFLocalizeExtension.Engine.GapTextControl
// Assembly: WPFLocalizeExtension, Version=3.4.0.0, Culture=neutral, PublicKeyToken=c726e0262981a1eb
// MVID: F4427321-B09E-4885-97B0-C6FD115FED01
// Assembly location: P:\Sébastien\GitHub\SizeOnDisk\References\WPFLocalizeExtension.3.4.0-alpha0039\lib\net452\WPFLocalizeExtension.dll

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace WPFLocalizeExtension.Engine
{
  /// <summary>A gap text control.</summary>
  [TemplatePart(Name = "PART_TextBlock", Type = typeof (TextBlock))]
  public class GapTextControl : Control
  {
    private TextBlock _theTextBlock = new TextBlock();
    /// <summary>
    /// This property is the string that may contain gaps for controls.
    /// </summary>
    public static readonly DependencyProperty FormatStringProperty = DependencyProperty.Register(nameof (FormatString), typeof (string), typeof (GapTextControl), new PropertyMetadata((object) string.Empty, new PropertyChangedCallback(GapTextControl.OnFormatStringChanged)));
    /// <summary>
    /// If this property is set to true there is no error thrown
    /// when the FormatString contains less gaps than placeholders are available.
    /// Missing placeholders for available elements may be a problem,
    /// as something else may refer to the element in a binding e.g. by name,
    /// but the element is not available in the visual tree.
    /// 
    /// As an example consider a submit button would be missing due to a missing placeholder in the FormatString.
    /// </summary>
    public static readonly DependencyProperty IgnoreLessGapsProperty = DependencyProperty.Register(nameof (IgnoreLessGaps), typeof (bool), typeof (GapTextControl), new PropertyMetadata((object) false));
    /// <summary>
    /// If this property is true, any FormatString that refers to the same string item multiple times produces an exception.
    /// </summary>
    public static readonly DependencyProperty IgnoreDuplicateStringReferencesProperty = DependencyProperty.Register(nameof (IgnoreDuplicateStringReferences), typeof (bool), typeof (GapTextControl), new PropertyMetadata((object) true));
    /// <summary>
    /// If this property is true, any FormatString that refers to the same control item multiple times produces an exception.
    /// </summary>
    public static readonly DependencyProperty IgnoreDuplicateControlReferencesProperty = DependencyProperty.Register(nameof (IgnoreDuplicateControlReferences), typeof (bool), typeof (GapTextControl), new PropertyMetadata((object) false));
    /// <summary>
    /// property that stores the items to be inserted into the gaps.
    /// any item that can be inserted as such into the TextBox get's inserted itself.
    /// All other items are converted to Text using their ToString() implementation.
    /// </summary>
    public static readonly DependencyProperty GapsProperty = DependencyProperty.Register(nameof (Gaps), typeof (ObservableCollection<object>), typeof (GapTextControl), new PropertyMetadata((object) null, new PropertyChangedCallback(GapTextControl.OnGapsChanged)));
    /// <summary>
    /// Pattern to split the FormatString, see https://github.com/SeriousM/WPFLocalizationExtension/issues/78#issuecomment-163023915 for documentation ( TODO!!!)
    /// </summary>
    public const string RegexPattern = "(.*?){(\\d*)}";
    private const string PART_TextBlock = "PART_TextBlock";

    private static void OnGapsChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
      if (dependencyPropertyChangedEventArgs.OldValue == dependencyPropertyChangedEventArgs.NewValue)
        return;
      ((GapTextControl) dependencyObject).OnContentChanged();
    }

    static GapTextControl()
    {
      FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof (GapTextControl), (PropertyMetadata) new FrameworkPropertyMetadata((object) typeof (GapTextControl)));
    }

    /// <summary>Creates a new instance.</summary>
    public GapTextControl()
    {
      this.Gaps = new ObservableCollection<object>();
      this.Gaps.CollectionChanged += (NotifyCollectionChangedEventHandler) ((sender, args) => this.OnContentChanged());
    }

    /// <summary>Gets or set the format string.</summary>
    public string FormatString
    {
      get
      {
        return (string) this.GetValue(GapTextControl.FormatStringProperty);
      }
      set
      {
        this.SetValue(GapTextControl.FormatStringProperty, (object) value);
      }
    }

    /// <summary>Ignore the Less Gaps</summary>
    public bool IgnoreLessGaps
    {
      get
      {
        return (bool) this.GetValue(GapTextControl.IgnoreLessGapsProperty);
      }
      set
      {
        this.SetValue(GapTextControl.IgnoreLessGapsProperty, (object) value);
      }
    }

    /// <summary>Ignore Duplicate String References</summary>
    public bool IgnoreDuplicateStringReferences
    {
      get
      {
        return (bool) this.GetValue(GapTextControl.IgnoreDuplicateStringReferencesProperty);
      }
      set
      {
        this.SetValue(GapTextControl.IgnoreDuplicateStringReferencesProperty, (object) value);
      }
    }

    /// <summary>Ignore Duplicate Control References</summary>
    public bool IgnoreDuplicateControlReferences
    {
      get
      {
        return (bool) this.GetValue(GapTextControl.IgnoreDuplicateControlReferencesProperty);
      }
      set
      {
        this.SetValue(GapTextControl.IgnoreDuplicateControlReferencesProperty, (object) value);
      }
    }

    /// <summary>Gets or sets the gap collection.</summary>
    public ObservableCollection<object> Gaps
    {
      get
      {
        return (ObservableCollection<object>) this.GetValue(GapTextControl.GapsProperty);
      }
      set
      {
        this.SetValue(GapTextControl.GapsProperty, (object) value);
      }
    }

    private static void OnFormatStringChanged(
      DependencyObject d,
      DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue == e.NewValue)
        return;
      ((GapTextControl) d).OnContentChanged();
    }

    private static T DeepCopy<T>(T obj) where T : DependencyObject
    {
      T obj1 = (T) XamlReader.Load((XmlReader) new XmlTextReader((TextReader) new StringReader(XamlWriter.Save((object) obj))));
      LocalValueEnumerator localValueEnumerator = obj.GetLocalValueEnumerator();
      while (localValueEnumerator.MoveNext())
      {
        DependencyProperty property = localValueEnumerator.Current.Property;
        BindingExpression bindingExpression = BindingOperations.GetBindingExpression((DependencyObject) obj, property);
        if (bindingExpression?.ParentBinding?.Path != null)
          BindingOperations.SetBinding((DependencyObject) obj1, property, (BindingBase) bindingExpression.ParentBinding);
      }
      return obj1;
    }

    private void OnContentChanged()
    {
      this._theTextBlock.Inlines.Clear();
      if (this.FormatString == null)
        throw new Exception("FormatString is not a string!");
      int startIndex = 0;
      if (this.Gaps != null)
      {
        Match match = Regex.Match(this.FormatString, "(.*?){(\\d*)}");
        while (match.Success)
        {
          string str = match.Groups[0].Value;
          string format = match.Groups[1].Value;
          int index = int.Parse(match.Groups[2].Value);
          startIndex += str.Length;
          match = match.NextMatch();
          this._theTextBlock.Inlines.Add(string.Format(format, (object) this.Gaps));
          if (this.Gaps.Count > index)
          {
            object gap = this.Gaps[index];
            try
            {
              if (gap is UIElement uiElement)
                this._theTextBlock.Inlines.Add(GapTextControl.DeepCopy<UIElement>(uiElement));
              else if (gap is Inline)
                this._theTextBlock.Inlines.Add(GapTextControl.DeepCopy<Inline>((Inline) gap));
              else if (gap != null)
                this._theTextBlock.Inlines.Add(gap.ToString());
            }
            catch (Exception ex)
            {
            }
          }
        }
      }
      this._theTextBlock.Inlines.Add(string.Format(this.FormatString.Substring(startIndex), (object) this.Gaps));
      this.InvalidateVisual();
    }

    /// <summary>Will be called prior to display of the control.</summary>
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      this.AttachToVisualTree();
    }

    private void AttachToVisualTree()
    {
      if (this.Template == null)
        return;
      TextBlock name = this.Template.FindName("PART_TextBlock", (FrameworkElement) this) as TextBlock;
      if (name == this._theTextBlock)
        return;
      this._theTextBlock = name;
      this.OnContentChanged();
    }
  }
}
