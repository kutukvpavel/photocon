using Avalonia.Controls;
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
}