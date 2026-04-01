using System;
using photocon.Grbl;

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
        public event EventHandler<float>? PositionChanged;
        public event EventHandler<MotionControlStates>? StateChanged;

        public MotionControl(SocketAdapter port)
        {
            Port = port;
            Port.DataReceived += Socket_DataReceived;
        }

        protected const int AutoReportOff = 0;
        protected SocketAdapter Port;
        protected float LastPosition = float.NaN;
        protected States LastState = States.Unknown;

        protected void Socket_DataReceived(object? sender, string e)
        {
            var responseType = Parser.GetResponseType(e);
            switch (responseType)
            {
                case ResponseTypes.StatusReport:
                    ProcessStatusReport(Parser.ParseStatusReport(e));
                    break;
                case ResponseTypes.Alarm:
                case ResponseTypes.Error:
                    State = MotionControlStates.Malfunction;
                    break;
                default: break;
            }
        }
        protected void SetAutoReport(int interval)
        {
            Port.Send($"$Report/Interval={interval}");
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
                PositionChanged?.Invoke(this, LastPosition);
            }
        }

        public MotionControlStates State { get; private set; } = MotionControlStates.Unhomed;
    }
}