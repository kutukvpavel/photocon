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
    public class DropOutDistinctStack<T> : LinkedList<T> where T : IEquatable<T>
    {
        private readonly int _capacity;
        protected LinkedListNode<T>? _current = null;

        public DropOutDistinctStack(int capacity) : base()
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
                if ((_current != null) && (this.Last == _current))
                {
                    _current = this.Last.Previous;
                }
                this.RemoveLast();
            }
            AddFirst(item);
        }

        public T? PeekNext()
        {
            if (_current == null)
            {
                _current = this.First;
            }
            else if (_current.Next != null)
            {
                _current = _current.Next;
            }
            return (_current == null) ? default : _current.Value;
        }

        public T? PeekPrevious()
        {
            if (_current == null) return default;
            _current = _current.Previous;
            return (_current == null) ? default : _current.Value;
        }

        public void ResetPeek()
        {
            _current = null;
        }
    }

    protected TerminalViewModel? LastDataContext = null;
    protected DropOutDistinctStack<string> History = new(100);
    protected string? IncompleteLine = null;

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
        txtInput.Text = string.Empty;
        IncompleteLine = null;
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
                break;
            case Key.Up:
                IncompleteLine = txtInput.Text.Length > 0 ? txtInput.Text : null;
                p = History.PeekNext();
                break;
            case Key.Down:
                p = History.PeekPrevious();
                if (p == null && IncompleteLine != null)
                {
                    p = IncompleteLine;
                    IncompleteLine = null;
                }
                break;
            default: return;
        }
        if (p != null) txtInput.Text = p;
    }
}