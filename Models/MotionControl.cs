using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using photocon.Grbl;
using SimpleTCP;

namespace photocon.Models
{
    public enum MotionControlStates
    {
        Unhomed,
        Homing,
        Homed,
        MovingToStart,
        WaitingAtStart,
        MovingToEnd,
        End,
        Malfunction
    }

    public class MotionControl
    {
        public event EventHandler<TimestampedResult>? PositionChanged;
        public event EventHandler<MotionControlStates>? StateChanged;
        public event EventHandler<string>? TerminalLineReceived;

        public static async Task<MotionControl?> Create(string host, int port, int autoReportInterval, int timeout = 2000)
        {
            var cancel = new CancellationTokenSource();
            try
            {
                cancel.CancelAfter(timeout);
                var client = await new SimpleTcpClient().Connect(host, port, cancel.Token);
                return new MotionControl(client, autoReportInterval);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        protected MotionControl(SimpleTcpClient port, int autoReportInterval)
        {
            Port = port;
            Port.StringEncoder = Encoding.ASCII;
            Port.Delimiter = (byte)'\n';
            Port.DelimiterDataReceived += Socket_DataReceived;
            WriteWithTerminal($"$Report/Interval={autoReportInterval}").Wait();
        }

        protected const int AutoReportOff = 0;
        protected float LastPosition = float.NaN;
        protected States LastState = States.Unknown;
        protected SimpleTcpClient Port;
        protected CancellationTokenSource Cancellation = new();

        protected void Socket_DataReceived(object? sender, Message e)
        {
            string s = e.MessageString.Trim('\r');
            TerminalLineReceived?.Invoke(this, s);
            var responseType = Parser.GetResponseType(s);
            switch (responseType)
            {
                case ResponseTypes.StatusReport:
                    ProcessStatusReport(Parser.ParseStatusReport(s));
                    break;
                case ResponseTypes.Alarm:
                case ResponseTypes.Error:
                    State = MotionControlStates.Malfunction;
                    break;
                default: break;
            }
        }
        protected void ProcessStatusReport(StatusReport sr)
        {
            if (sr.State != LastState)
            {
                if (State != MotionControlStates.Unhomed && sr.State == States.Home)
                {
                    State = MotionControlStates.Homing;
                }
                switch (State)
                {
                    case MotionControlStates.Unhomed:
                        switch (sr.State)
                        {
                            case States.Run:
                            case States.Home:
                                State = MotionControlStates.Homing;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.Homing:
                        switch (sr.State)
                        {
                            case States.Idle:
                                State = MotionControlStates.Homed;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.Homed:
                        switch (sr.State)
                        {
                            case States.Run:
                                State = MotionControlStates.MovingToStart;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.MovingToStart:
                        switch (sr.State)
                        {
                            case States.Idle:
                                State = MotionControlStates.WaitingAtStart;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.WaitingAtStart:
                        switch (sr.State)
                        {
                            case States.Run:
                                State = MotionControlStates.MovingToEnd;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.MovingToEnd:
                        switch (sr.State)
                        {
                            case States.Idle:
                                State = MotionControlStates.End;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.End:
                        switch (sr.State)
                        {
                            case States.Run:
                                State = MotionControlStates.MovingToStart;
                                break;
                            default: break;
                        }
                        break;
                    case MotionControlStates.Malfunction:
                        switch (sr.State)
                        {
                            case States.Idle:
                            case States.Run:
                                State = MotionControlStates.Unhomed;
                                break;
                            default: break;
                        }
                        break;
                    default: break;
                }
                LastState = sr.State;
                if (LastState == States.Other) State = MotionControlStates.Malfunction;
                StateChanged?.Invoke(this, State);
            }
            if (sr.Position != LastPosition)
            {
                LastPosition = sr.Position;
                PositionChanged?.Invoke(this, new TimestampedResult(LastPosition));
            }
        }
        protected async Task WriteWithTerminal(string cmd)
        {
            TerminalLineReceived?.Invoke(this, cmd);
            await Port.WriteLine(cmd);
        }

        public MotionControlStates State { get; private set; } = MotionControlStates.Unhomed;

        public async Task ExecuteStateMachine(ScanParams p)
        {
            switch (State)
            {
                case MotionControlStates.Unhomed:
                    await WriteWithTerminal("$H");
                    break;
                case MotionControlStates.Homed:
                    await WriteWithTerminal(string.Format(CultureInfo.InvariantCulture, "G0 X{0:F2}", p.Start));
                    break;
                case MotionControlStates.WaitingAtStart:
                    await WriteWithTerminal(string.Format(CultureInfo.InvariantCulture, "G1 X{0:F2} F{1:F4}", p.End, p.Speed));
                    break;
                case MotionControlStates.End:
                    await WriteWithTerminal(string.Format(CultureInfo.InvariantCulture, "G0 X{0:F2}", p.Start));
                    break;
                case MotionControlStates.Malfunction:
                    State = MotionControlStates.Unhomed; //Reset any error, expect manual handling of this situation
                    break;
                default: break;
            }
        }
        public void ForceSkipHoming()
        {
            if (State == MotionControlStates.Unhomed) State = MotionControlStates.Homed;
        }
        public void Close()
        {
            Port.Disconnect();
        }
        public async Task SendManualCommand(string cmd)
        {
            await WriteWithTerminal(cmd);
        }
        public async Task AbortMotion()
        {
            await WriteWithTerminal("!");
            State = MotionControlStates.Malfunction;
            StateChanged?.Invoke(this, State);
        }
    }
}