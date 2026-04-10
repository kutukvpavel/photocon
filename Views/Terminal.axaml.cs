using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using photocon.ViewModels;

namespace photocon.Views;

public partial class Terminal : UserControl
{
    protected TerminalViewModel? LastDataContext = null;

    public Terminal()
    {
        InitializeComponent();
        txtTerminal.TextChanged += OnDisplayTextChanged;
    }

    protected void Send_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as TerminalViewModel)?.RequestSending();
    }

    protected void OnDisplayTextChanged(object? sender, TextChangedEventArgs e)
    {
        txtTerminal.CaretIndex = txtTerminal.Text?.Length ?? 0;
    }

    protected void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Send_Click(this, new RoutedEventArgs());
        txtInput.Text = string.Empty;
    }
}