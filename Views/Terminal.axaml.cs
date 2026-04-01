using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace photocon.Views;

public partial class Terminal : UserControl
{
    public Terminal()
    {
        InitializeComponent();
    }

    public void Send_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as ViewModels.TerminalViewModel)?.RequestSending();
    }
}