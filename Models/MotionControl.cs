namespace photocon.Models
{
    public enum MotionControlStates
    {
        Unhomed,
        Homing,
        Homed,
        MovingToStart,
        MovingToEnd,
        End
    }

    public class MotionControl
    {
        public MotionControl(SocketAdapter port)
        {
            Port = port;
        }

        protected SocketAdapter Port;

        public MotionControlStates State { get; private set; }
    }
}