namespace photocon.Models
{
    public class Electrometer
    {
        public Electrometer(SocketAdapter port)
        {
            Port = port;
        }

        protected SocketAdapter Port;
    }
}