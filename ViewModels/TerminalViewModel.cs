using System;
using System.Text;
using ReactiveUI;

namespace photocon.ViewModels;

public class TerminalViewModel : ViewModelBase
{
    public event EventHandler<string>? SendRequested;

    public TerminalViewModel()
    {

    }

    public string TerminalText => _TerminalText.ToString();
    public string ManualSendText
    {
        get => _ManualSendText;
        set
        {
            _ManualSendText = value;
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }
    public bool CanSend => ManualSendText.Length > 0;

    public void AppendLine(string l)
    {
        _TerminalText.AppendLine(l);
        this.RaisePropertyChanged(nameof(TerminalText));
    }

    public void RequestSending()
    {
        if (CanSend) SendRequested?.Invoke(this, ManualSendText);
    }

    private StringBuilder _TerminalText = new();
    private string _ManualSendText = string.Empty;
}
