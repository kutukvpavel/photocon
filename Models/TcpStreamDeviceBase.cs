using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace photocon.Models
{
    public abstract class TcpStreamDeviceBase
    {
        protected static async Task<SimpleTcpClient?> Connect(string host, int port, int timeout = 2000)
		{
			var cancel = new CancellationTokenSource();
            try
            {
                cancel.CancelAfter(timeout);
                return await new SimpleTcpClient().Connect(host, port, cancel.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
		}

        protected TcpStreamDeviceBase(SimpleTcpClient port)
        {
            Port = port;
            Port.StringEncoder = Encoding.ASCII;
            Port.Delimiter = (byte)'\n';
            Port.DelimiterDataReceived += Socket_DataReceived;
        }

        protected SimpleTcpClient Port;
        protected CancellationTokenSource Cancellation = new();

        protected void Socket_DataReceived(object? sender, Message e)
        {
            string s = e.MessageString.Trim('\r');
            TerminalLineReceived?.Invoke(this, s);
            ProcessReceivedLine(s);
        }

        protected async Task WriteWithTerminal(string cmd)
        {
            TerminalLineReceived?.Invoke(this, cmd);
            await Port.WriteLine(cmd);
        }

        protected abstract void ProcessReceivedLine(string s);

        public event EventHandler<string>? TerminalLineReceived;

        public void Close()
        {
            Port.Disconnect();
        }
        public async Task SendManualCommand(string cmd)
        {
            await WriteWithTerminal(cmd);
        }
    }
}