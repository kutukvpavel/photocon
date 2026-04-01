using System;
using System.Reactive;
using ReactiveUI;
using photocon.Models;
using System.Text;
using System.Threading.Tasks;

namespace photocon.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(Settings cfg)
    {
        Configuration = cfg;
    }

    public MotionControl? MotionControlContext { get; private set; }
    public Electrometer? ElectrometerContext { get; private set; }
    public ScanParams ScanParamsContext { get; private set; } = new ScanParams();
    public Settings Configuration { get; }

    public TerminalViewModel GrblTerminalContext { get; } = new();
    public TerminalViewModel ScpiTerminalContext { get; } = new();
    public bool IsConnected { get; private set; } = false;

    public void SetScanParams(ScanParams scanParams)
    {
        ScanParamsContext = scanParams;
        this.RaisePropertyChanged(nameof(ScanParamsContext));
    }

    public async Task Connect()
    {
        try
        {
            
        }
        catch (System.Exception)
        {
            
            throw;
        }
        MotionControlContext = new MotionControl(Configuration.FluidNcIp, Configuration.FluidNcPort, Configuration.FluidNcAutoReportIntervalMs);
        MotionControlContext.PositionChanged += OnPositionChanged;
        MotionControlContext.StateChanged += OnStateChanged;
        ElectrometerContext = await Electrometer.Create(Configuration.ElectrometerIp, Configuration.ElectrometerPort);
        ElectrometerContext.PollIntervalMs = Configuration.ElectrometerPollIntervalMs;
        ElectrometerContext.ResultReceived += OnReadingReceived;
        this.RaisePropertyChanged(nameof(MotionControlContext));
        this.RaisePropertyChanged(nameof(ElectrometerContext));
        this.RaisePropertyChanged(nameof(IsConnected));
    }

    protected void OnPositionChanged(object? sender, float p)
    {
        
    }
    protected void OnStateChanged(object? sender, MotionControlStates s)
    {
        
    }
    protected void OnReadingReceived(object? sender, TimestampedResult r)
    {
        
    }
}
