using System;
using System.Reactive;
using ReactiveUI;
using photocon.Models;

namespace photocon.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(Settings cfg)
    {
        Configuration = cfg;
    }

    public MotionControl MotionControlContext { get; private set; }
    public Electrometer ElectrometerContext { get; private set; }
    public ScanParams ScanParamsContext { get; private set; }
    public Settings Configuration { get; }

    public void SetScanParams(ScanParams scanParams)
    {
        ScanParamsContext = scanParams;
        this.RaisePropertyChanged(nameof(ScanParamsContext));
    }
}
