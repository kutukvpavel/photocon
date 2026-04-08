using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace photocon.Models
{
    public class Electrometer : TcpStreamDeviceBase
    {
        public static async Task<Electrometer?> Create(string host, int port, int timeout = 2000)
        {
            var socket = await Connect(host, port, timeout);
            if (socket != null) return new Electrometer(socket);
            else return null;
        }
        protected static double ConvertReading(string response)
        {
            try
            {
                string[] splt = response.Split(',', 2);
                return double.Parse(splt[0], CultureInfo.InvariantCulture);   
            }
            catch (Exception ex)
            {
                Program.LogException(ex, "Failed to convert reading to double");
                return double.NaN;
            }
        }

        public event EventHandler<TimestampedResult>? ResultReceived;
        
        protected Electrometer(SimpleTcpClient port)
            : base(port)
        {
            
        }

        protected void Poll()
        {
            var token = Cancellation.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!PollSemaphore!.Wait(PollIntervalMs, token))
                    {
                        Program.LogException(new TimeoutException(), "Electrometer output processing took too long");
                    }
                    WriteWithTerminal(":READ?").Wait();
                    token.WaitHandle.WaitOne(PollIntervalMs);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        protected override void ProcessReceivedLine(string s)
        {
            if ((PollSemaphore?.CurrentCount ?? -1) != 0) return;
            double reading = ConvertReading(s);
            if (!double.IsNaN(reading)) ResultReceived?.Invoke(this, new TimestampedResult(reading));
            try
            {
                PollSemaphore!.Release();
            }
            catch (SemaphoreFullException ex)
            {
                Program.LogException(ex, "Should never happen (or a race condition)");
            }
        }

        protected Task? PollingTask;
        protected SemaphoreSlim? PollSemaphore;

        public int PollIntervalMs { get; set; } = 1000;
        public bool IsPolling { get; private set; } = false;

        public void StartPoll()
        {
            if (IsPolling) return;
            PollSemaphore = new(1, 1);
            PollingTask = Task.Run(Poll);
            IsPolling = true;
        }
        public async Task StopPoll()
        {
            if (!IsPolling) return;
            Cancellation.Cancel();
            while ((PollingTask?.Status ?? TaskStatus.RanToCompletion) == TaskStatus.Running)
            {
                await Task.Delay(50);
            }
            Cancellation = new CancellationTokenSource();
            IsPolling = false;
            PollingTask = null;
            PollSemaphore?.Dispose();
            PollSemaphore = null;
        }
    }
}