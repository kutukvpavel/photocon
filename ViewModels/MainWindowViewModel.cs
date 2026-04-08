using System;
using System.Threading.Tasks;
using ReactiveUI;
using photocon.Models;

namespace photocon.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    protected enum UiStates
    {
        NotConnected,
        Connecting,
        Ready,
        AcqiringSpectrum,
        Finished
    }
    protected const string NotAvailable = "N/A";

    protected MotionControlStates _InternalState = MotionControlStates.Unhomed;
    protected UiStates _InternalUiState = UiStates.NotConnected;

    public MainWindowViewModel(Settings cfg)
    {
        Configuration = cfg;
        ScanParamsContext.BacklashCorrection = Configuration.BacklashCompensationNm;
        Logger = new(Environment.CurrentDirectory);
        SpectrumData = new(ScanParamsContext);
        GrblTerminalContext.SendRequested += OnGrblManualSendRequsted;
        ScpiTerminalContext.SendRequested += OnScpiManualSendRequuested;
        Electrometer.ConnectionTerminalLineReceived += OnElectrometerTerminal;
    }

    public MotionControl? MotionControlContext { get; private set; }
    public Electrometer? ElectrometerContext { get; private set; }
    public ScanParams ScanParamsContext { get; private set; } = new ScanParams();
    public Settings Configuration { get; }
    public DataLogger Logger { get; }

    public TerminalViewModel GrblTerminalContext { get; } = new();
    public TerminalViewModel ScpiTerminalContext { get; } = new();
    public bool IsConnected => (_InternalUiState != UiStates.NotConnected) && (_InternalUiState != UiStates.Connecting);
    public Spectrum SpectrumData { get; }
    public string StateString
    {
        get
        {
            return _InternalUiState switch
            {
                UiStates.Ready or UiStates.AcqiringSpectrum => Enum.GetName(_InternalState) ?? NotAvailable,
                _ => Enum.GetName(_InternalUiState) ?? NotAvailable
            };
        }
    }
    public string NextStateString
    {
        get
        {
            return _InternalUiState switch
            {
                UiStates.NotConnected or UiStates.Connecting => "Connect",
                UiStates.Ready or UiStates.AcqiringSpectrum => _InternalState switch
                {
                    MotionControlStates.Unhomed => "Home",
                    MotionControlStates.Homed or MotionControlStates.End => "Move to Start",
                    MotionControlStates.WaitingAtStart => "Acquire Spectrum!",
                    MotionControlStates.Malfunction => "Reset",
                    _ => NotAvailable
                },
                UiStates.Finished => "Save Spectrum",
                _ => NotAvailable
            };
        }
    }
    public bool CanAdvanceStateMachine => _InternalUiState switch
    {
        UiStates.Ready or UiStates.AcqiringSpectrum => _InternalState switch
        {
            MotionControlStates.Unhomed or MotionControlStates.Homed or MotionControlStates.WaitingAtStart or MotionControlStates.End or MotionControlStates.Malfunction => true,
            _ => false
        },
        _ => true
    } && !IsBusy;
    public bool CanForceSkipState => _InternalState == MotionControlStates.Unhomed && IsConnected;
    public bool CanSaveSpectrum =>
        _InternalUiState == UiStates.Finished || (_InternalState == MotionControlStates.Malfunction && !SpectrumData.IsEmpty);
    public bool CanEditParameters => _InternalUiState != UiStates.AcqiringSpectrum;
    protected bool _IsBusy = false;
    public bool IsBusy
    { 
        get => _IsBusy;
        private set
        {
            if (_IsBusy != value)
            _IsBusy = value;
            this.RaisePropertyChanged(nameof(IsBusy));
            this.RaisePropertyChanged(nameof(CanAdvanceStateMachine));
            this.RaisePropertyChanged(nameof(CanAbort));
        }
    }
    public bool CanAbort => IsConnected && IsBusy;

    public void SetScanParams(ScanParams scanParams)
    {
        ScanParamsContext = scanParams;
        ScanParamsContext.BacklashCorrection = Configuration.BacklashCompensationNm;
        this.RaisePropertyChanged(nameof(ScanParamsContext));
    }
    public async Task AdvanceStateMachine(string? arg = null)
    {
        if (!CanAdvanceStateMachine) return;
        switch (_InternalUiState)
        {
            case UiStates.NotConnected:
            {
                if (_InternalUiState != UiStates.NotConnected) return;
                _InternalUiState = UiStates.Connecting;
                IsBusy = true;
                this.RaisePropertyChanged(nameof(StateString));
                this.RaisePropertyChanged(nameof(NextStateString));
                bool success = false;
                try
                {
                    MotionControlContext = await MotionControl.Create(Configuration.FluidNcIp, Configuration.FluidNcPort, Configuration.FluidNcAutoReportIntervalMs);
                    if (MotionControlContext == null) throw new TimeoutException();
                    MotionControlContext.PositionChanged += OnPositionChanged;
                    MotionControlContext.StateChanged += OnStateChanged;
                    MotionControlContext.TerminalLineReceived += OnMotionTerminal;
                    success = true;
                }
                catch (Exception ex)
                {
                    Program.LogException(ex, "Failed to connect to motion control");
                }
                if (success)
                {
                    success = false;
                    try
                    {

                        ElectrometerContext = await Electrometer.Create(Configuration.ElectrometerIp, Configuration.ElectrometerPort);
                        ElectrometerContext.PollIntervalMs = Configuration.ElectrometerPollIntervalMs;
                        ElectrometerContext.ResultReceived += OnReadingReceived;
                        ElectrometerContext.TerminalLineReceived += OnElectrometerTerminal;
                        success = true; 
                    }
                    catch (Exception ex)
                    {
                        Program.LogException(ex, "Failed to connect to electrometer");
                        MotionControlContext?.Close();
                        MotionControlContext = null;
                    }
                }
                if (success)
                {
                    _InternalUiState = UiStates.Ready;
                    OnStateChanged(this, MotionControlStates.Unhomed);
                    this.RaisePropertyChanged(nameof(IsConnected));
                }
                else
                {
                    _InternalUiState = UiStates.NotConnected;
                }
                IsBusy = false;
                break;
            }
            case UiStates.Finished:
            {
                if (arg == null) throw new NullReferenceException();
                await SaveSpectrumInternal(arg);
                _InternalUiState = UiStates.Ready;
                break;
            }
            default:
            {
                bool _busy = IsBusy;
                IsBusy = true;
                if (_InternalUiState == UiStates.Ready && _InternalState == MotionControlStates.WaitingAtStart)
                {
                    await Logger.CreateNewBackupFile();
                    SpectrumData.SetAcquisitionParameters(ScanParamsContext);
                    ElectrometerContext!.StartPoll();
                    _InternalUiState = UiStates.AcqiringSpectrum;
                }
                await MotionControlContext!.ExecuteStateMachine(ScanParamsContext);
                if (_InternalUiState != UiStates.AcqiringSpectrum) IsBusy = _busy;
                break;
            }
        }
        UpdateUiStates();
    }
    public void ForceSkipHoming()
    {
        if (MotionControlContext == null || !CanForceSkipState) return;
        MotionControlContext.ForceSkipHoming();
        UpdateUiStates();
    }
    public async Task SaveSpectrum(string path)
    {
        if (!CanSaveSpectrum) return;
        if (_InternalUiState == UiStates.Finished)
        {
            await AdvanceStateMachine(path);
        }
        else //Malfunction
        {
            await SaveSpectrumInternal(path);
        }
    }
    public async Task Abort()
    {
        MotionControlContext?.AbortMotion();
        if (ElectrometerContext != null) await ElectrometerContext.StopPoll();
    }

    protected async Task SaveSpectrumInternal(string path)
    {
        await DataLogger.SaveSpectrum(SpectrumData, path);
    }
    protected void UpdateUiStates()
    {
        this.RaisePropertyChanged(nameof(StateString));
        this.RaisePropertyChanged(nameof(NextStateString));
        this.RaisePropertyChanged(nameof(CanAdvanceStateMachine));
        this.RaisePropertyChanged(nameof(CanForceSkipState));
        this.RaisePropertyChanged(nameof(CanSaveSpectrum));
        this.RaisePropertyChanged(nameof(CanEditParameters));
        this.RaisePropertyChanged(nameof(IsBusy));
        this.RaisePropertyChanged(nameof(CanAbort));
    }
    protected void OnPositionChanged(object? sender, TimestampedResult p)
    {
        SpectrumData.UpdateX(p);
    }
    protected async void OnStateChanged(object? sender, MotionControlStates s)
    {
        if (_InternalState == MotionControlStates.MovingToEnd && s != MotionControlStates.MovingToEnd)
        {
            if (ElectrometerContext != null) await ElectrometerContext.StopPoll();
            if (s == MotionControlStates.End) _InternalUiState = UiStates.Finished;
        }
        _InternalState = s;
        if (_InternalState == MotionControlStates.Malfunction && _InternalUiState == UiStates.AcqiringSpectrum)
        {
            if (ElectrometerContext != null) await ElectrometerContext.StopPoll();
            _InternalUiState = UiStates.Ready;
        }
        IsBusy = s switch
        {
            MotionControlStates.Homing or MotionControlStates.MovingToStart or MotionControlStates.MovingToEnd => true,
            _ => false
        };
        UpdateUiStates();
    }
    protected void OnReadingReceived(object? sender, TimestampedResult r)
    {
        SpectrumData.AddY(r);
    }
    protected void OnGrblManualSendRequsted(object? sender, string e)
    {
        MotionControlContext?.SendManualCommand(e);
    }
    protected void OnScpiManualSendRequuested(object? sender, string e)
    {
        ElectrometerContext?.SendManualCommand(e);
    }
    protected void OnElectrometerTerminal(object? sender, string e)
    {
        ScpiTerminalContext.AppendLine(e);
    }
    protected void OnMotionTerminal(object? sender, string e)
    {
        GrblTerminalContext.AppendLine(e);
    }
}
