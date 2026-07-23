using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using photocon.ViewModels;

namespace photocon.Views;

public partial class Terminal : UserControl
{
    public class DropOutDistinctStack<T> : List<T> where T : IEquatable<T>
    {
        private readonly int _capacity;
        protected int _peekIndex = 0;

        public DropOutDistinctStack(int capacity)
        {
            _capacity = capacity;
        }

        public void Push(T item)
        {
            if (this.FirstOrDefault() != null)
            {
                if (item.Equals(this.First()))
                {
                    return;
                }
            }

            if (this.Count >= _capacity)
            {
                this.RemoveAt(Count - 1);
            }

            Add(item);
        }

        public T? PeekNext()
        {
            if (_peekIndex >= Count) 
            {
                _peekIndex = Count;
                return (this.LastOrDefault() == null) ? default : this.Last();
            }
            else if (_peekIndex < 0)
            {
                _peekIndex = 0;
            }
            return this[_peekIndex++];
        }

        public T? PeekPrevious()
        {
            if (_peekIndex >= Count)
            {
                _peekIndex = Count;
                return (this.LastOrDefault() == null) ? default : this.Last();   
            }
            else if (_peekIndex < 0)
            {
                _peekIndex = 0;
            }
            if (_peekIndex == 0) return default;
            return this[--_peekIndex];
        }

        public void ResetPeek()
        {
            _peekIndex = 0;
        }
    }

    protected TerminalViewModel? LastDataContext = null;
    protected DropOutDistinctStack<string> History = new(100);

    public Terminal()
    {
        InitializeComponent();
        DataContextChanged += DataContext_Changed;
    }

    protected void Send_Click(object? sender, RoutedEventArgs e)
    {
        History.Push(txtInput.Text);
        History.ResetPeek();
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
        Dispatcher.UIThread.InvokeAsync(() => txtTerminal.ScrollToLine(txtTerminal.GetLineCount() - 1));
    }

    protected void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        string? p = null;
        switch (e.Key)
        {
            case Key.Enter:
                Send_Click(this, new RoutedEventArgs());
                txtInput.Text = string.Empty;
                break;
            case Key.Up:
                p = History.PeekNext();
                break;
            case Key.Down:
                p = History.PeekPrevious();
                break;
            default: return;
        }
        if (p != null) txtInput.Text = p;
    }
}