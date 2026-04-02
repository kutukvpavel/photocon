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
        public static async Task<Electrometer> Create(IScpiConnection connection, ILogger<ScpiDevice>? logger = null, CancellationToken cancellationToken = default)
		{
			// Try to open the connection:
			logger?.LogInformation($"Opening TCP connection for device {connection.DevicePath}...");
			await connection.Open(cancellationToken);

			// Get device ID:
			logger?.LogInformation("Connection succeeded, trying to read device ID...");

			string id = await connection.GetId(cancellationToken);
			logger?.LogInformation($"Connection succeeded. Device id: {id}");

			// Create the driver instance.
			return new Electrometer(connection, id, logger);
		}

        public static async Task<Electrometer> Create(string host, int port)
        {
            return await Create(new TcpScpiConnection(host, port));
        }

        public event EventHandler<TimestampedResult>? ResultReceived;
        public event EventHandler<string>? TerminalLineReceived;
        
        protected Electrometer(IScpiConnection connection, string id, ILogger<ScpiDevice>? logger)
            : base(connection, id, logger)
        {
            
        }

        protected async Task<string> QueryWithTerminal(string cmd)
        {
            TerminalLineReceived?.Invoke(this, cmd);
            return await Query(cmd, Cancellation.Token);
        }
        protected async Task SendWithTerminal(string cmd)
        {
            TerminalLineReceived?.Invoke(this, cmd);
            await SendCmd(cmd);
        }

        protected async Task Poll()
        {
            try
            {
                while (!Cancellation.IsCancellationRequested)
                {
                    string s = await QueryWithTerminal(":READ?");
                    ResultReceived?.Invoke(this, new TimestampedResult(ConvertReading(s)));
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

        public CancellationTokenSource Cancellation { get; } = new();
        public int PollIntervalMs { get; set; } = 1000;

        public void StartPoll()
        {
            PollingTask = Task.Run(Poll);
        }
        public async Task StopPoll()
        {
            Cancellation.Cancel();
            while ((PollingTask?.Status ?? TaskStatus.RanToCompletion) == TaskStatus.Running)
            {
                await Task.Delay(50);
            }
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