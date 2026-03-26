using photocon.Grbl;

namespace photocon.Models
{
    public enum MotionControlStates
    {
        Unhomed,
        Homing,
        Homed,
        MovingToStart,
        MovingToEnd,
        End,
        Malfunction
    }

    public class MotionControl
    {
        public MotionControl(SocketAdapter port)
        {
            Port = port;
            Port.DataReceived += Socket_DataReceived;
        }

        protected const int AutoReportOff = 0;
        protected SocketAdapter Port;

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
            
        }

        public MotionControlStates State { get; private set; } = MotionControlStates.Unhomed;
    }
}