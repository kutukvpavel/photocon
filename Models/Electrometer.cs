using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScpiNet;

namespace photocon.Models
{
    public class Electrometer : ScpiDevice
    {
        public static EventHandler<string>? ConnectionTerminalLineReceived;
        public static int Timeout { get; set; } = 700;

        public static async Task<Electrometer> Create(IScpiConnection connection, ILogger<ScpiDevice>? logger = null, CancellationToken cancellationToken = default)
		{
			// Try to open the connection:
			logger?.LogInformation($"Opening TCP connection for device {connection.DevicePath}...");
			await connection.Open(cancellationToken);

			// Get device ID:
			logger?.LogInformation("Connection succeeded, trying to read device ID...");

            await connection.ClearBuffers(cancellationToken);
            //string cls = "*CLS";
            //await connection.WriteString(cls, true, cancellationToken);
            //ConnectionTerminalLineReceived?.Invoke(null, cls);
            ConnectionTerminalLineReceived?.Invoke(null, "*IDN?");
			string id = await connection.GetId(cancellationToken);
			logger?.LogInformation($"Connection succeeded. Device id: {id}");
            ConnectionTerminalLineReceived?.Invoke(null, id);

			// Create the driver instance.
			return new Electrometer(connection, id, logger) { StripHeaders = false };
		}

        public static async Task<Electrometer> Create(string host, int port)
        {
            return await Create(new TcpScpiConnection(host, port, Timeout));
        }

        public event EventHandler<TimestampedResult>? ResultReceived;
        public event EventHandler<string>? TerminalLineReceived;
        
        protected Electrometer(IScpiConnection connection, string id, ILogger<ScpiDevice>? logger)
            : base(connection, id, logger)
        {
            
        }

        protected async Task<string?> QueryWithTerminal(string cmd)
        {
            try
            {
                TerminalLineReceived?.Invoke(this, cmd);
                //Cancellation.CancelAfter(Timeout);
                string s = await Query(cmd, Cancellation.Token);
                //if (!Cancellation.TryReset()) Cancellation = new CancellationTokenSource();
                TerminalLineReceived?.Invoke(this, s);
                return s;
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                return null;
            }
        }
        protected async Task SendWithTerminal(string cmd)
        {
            TerminalLineReceived?.Invoke(this, cmd);
            await SendCmd(cmd);
        }

        protected async Task Poll()
        {
            var token = Cancellation.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string? s = await QueryWithTerminal(":READ?");
                    if (s != null)
                    {
                        ResultReceived?.Invoke(this, new TimestampedResult(ConvertReading(s)));
                    }
                    await Task.Delay(PollIntervalMs);
                }
            }
            catch (OperationCanceledException)
            {
                    
            }
        }

        protected double ConvertReading(string response)
        {
            try
            {
                return double.Parse(response, CultureInfo.InvariantCulture);   
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to convert reading to double");
                return double.NaN;
            }
        }

        protected Task? PollingTask;

        public CancellationTokenSource Cancellation { get; private set; } = new();
        public int PollIntervalMs { get; set; } = 1000;
        public bool IsPolling { get; private set; } = false;

        public void StartPoll()
        {
            if (IsPolling) return;
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
        }
        public async Task SendManualCommand(string cmd)
        {
            if (cmd.EndsWith('?'))
            {
                await QueryWithTerminal(cmd);
            }
            else
            {
                await SendWithTerminal(cmd);
            }
        }
    }
}