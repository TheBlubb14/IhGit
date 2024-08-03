using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IhGitWpf;

public class NumericTextBox : TextBox
{
    public const string DEFAULT = "0";

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        Text = int.TryParse(Text, NumberStyles.Integer, CultureInfo.CurrentUICulture, out var value) 
            ? value.ToString() 
            : DEFAULT;

        base.OnLostFocus(e);
    }
    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        e.Handled = !int.TryParse(Text, NumberStyles.Integer, CultureInfo.CurrentUICulture, out _);
        base.OnTextChanged(e);
    }
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, NumberStyles.Integer, CultureInfo.CurrentUICulture, out _);

        base.OnPreviewTextInput(e);
    }
}