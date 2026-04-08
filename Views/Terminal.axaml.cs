using System;
using System.ComponentModel;
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
        DataContextChanged += DataContext_Changed;
    }

    protected void Send_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as TerminalViewModel)?.RequestSending();
    }

    protected void DataContext_Changed(object? sender, EventArgs e)
    {
        if (LastDataContext != null) LastDataContext.PropertyChanged -= OnDisplayTextChanged;
        LastDataContext = DataContext as TerminalViewModel;
        if (LastDataContext != null) LastDataContext.PropertyChanged += OnDisplayTextChanged;
    }

    protected void OnDisplayTextChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LastDataContext.TerminalText)) return;
        txtTerminal.ScrollToLine(txtTerminal.GetLineCount() - 1);
    }

    protected void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Send_Click(this, new RoutedEventArgs());
        txtInput.Text = string.Empty;
    }
}