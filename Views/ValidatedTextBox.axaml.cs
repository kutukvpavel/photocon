using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace photocon.Views;

public partial class ValidatedTextBox : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<ValidatedTextBox, string>(nameof(Label), "");
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<ValidatedTextBox, double>(nameof(Value), 0);
    public static readonly StyledProperty<string> FormatStringProperty = 
        AvaloniaProperty.Register<ValidatedTextBox, string>(nameof(FormatString), "F3");
    public static readonly StyledProperty<bool> ShowLabelProperty = AvaloniaProperty.Register<ValidatedTextBox, bool>(nameof(ShowLabel), false);
    public static NumberStyles NumberStyle { get; set; } = NumberStyles.Float;

    public ValidatedTextBox()
    {
        InitializeComponent();

        if (Application.Current != null) Application.Current.ActualThemeVariantChanged += ActualThemeVariant_Changed;
        txtLabel.Bind(TextBlock.TextProperty, this.GetObservable(LabelProperty));
        txtLabel.Bind(TextBlock.IsVisibleProperty, this.GetObservable(ShowLabelProperty));
        this.GetObservable(ValueProperty).Subscribe((v) => txtValue.Text = v.ToString(FormatString));
        txtValue.TextChanged += txtValue_TextChanged;
        txtValue.LostFocus += (o, e) => AssignTemporaryValue();
        txtValue.KeyDown += (o, e) => AssignTemporaryValue();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set 
        {
            SetValue(LabelProperty, value);
            SetValue(ShowLabelProperty, value.Length > 0);
        }
    }
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }
    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    private double? TemporaryValue;
    private void txtValue_TextChanged(object? sender, TextChangedEventArgs e)
    {
        bool parsed = double.TryParse(txtValue.Text, NumberStyle, CultureInfo.CurrentUICulture, out double v);
        txtValue.Background = parsed ? App.GreenOK : App.OrangeWarning;
        if (parsed) {
            if (!txtValue.IsFocused) 
            {
                TemporaryValue = null;
                Value = v;
            }
            else
            {
                TemporaryValue = v;
            }
            DataValidationErrors.ClearErrors(txtValue);
        }
        else
        {
            DataValidationErrors.SetErrors(txtValue, new string[] { "Incorrect Format" });
        }
    }
    private void AssignTemporaryValue()
    {
        if (TemporaryValue.HasValue) Value = TemporaryValue.Value;
    }
    private void ActualThemeVariant_Changed(object? sender, EventArgs e)
    {
        txtValue.Background = DataValidationErrors.GetHasErrors(txtValue) ? App.OrangeWarning : App.GreenOK;
    }
}