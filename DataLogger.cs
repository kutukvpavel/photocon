using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using photocon.Models;

namespace photocon
{
    public class DataLogger
    {
        public static string BackupSubfolderName { get; set; } = "backup";

        public DataLogger(string folderPath)
        {
            _FolderPath = folderPath;
            string bcpFolder = Path.Combine(folderPath, BackupSubfolderName);
            if (!Directory.Exists(bcpFolder))
            {
                Directory.CreateDirectory(bcpFolder);
            }
        }

        public async Task CreateNewElectrometerBackupFile()
        {
            await CreateBackupFile(_BackupElectrometerWriter, _BackupElectrometerCsvWriter, "Electro");
        }
        public async Task CreateNewPositionBackupFile()
        {
            await CreateBackupFile(_BackupPositionWriter, _BackupPositionCsvWriter, "Position");
        }

        public async Task LogElectrometerPointBackup(TimestampedResult r)
        {
            await LogPointBackup(_BackupElectrometerCsvWriter, CreateNewElectrometerBackupFile, r);
        }
        public async Task LogPositionPointBackup(TimestampedResult r)
        {
            await LogPointBackup(_BackupPositionCsvWriter, CreateNewPositionBackupFile, r);
        }

        public static async Task SaveSpectrum(Spectrum s, string path)
        {
            using TextWriter tw = new StreamWriter(path);
            using CsvWriter cw = new(tw, CultureInfo.InvariantCulture);
            await cw.NextRecordAsync();
            cw.WriteField(
                $"Acquisition params: Start = {s.AcquisitionParameters.Start} nm, End = {s.AcquisitionParameters.End} nm, Speed = {s.AcquisitionParameters.Speed} nm/min"
                );
            await cw.NextRecordAsync();
            cw.WriteField("Wavelength (nm)");
            cw.WriteField("Conductance");
            cw.WriteField("Time");
            cw.WriteField("Conductance");
            cw.WriteField("Time");
            cw.WriteField("Time Discrepancy (s)");
            await cw.NextRecordAsync();
            int length = s.MaxLength;
            for (int i = 0; i < length; i++)
            {
                if (s.PositionDomainPoints.Count > i)
                {
                    var pair = s.PositionDomainPoints.ElementAt(i);
                    cw.WriteField(pair.Key);
                    cw.WriteField(pair.Value);
                }
                else
                {
                    cw.WriteField(string.Empty);
                    cw.WriteField(string.Empty);
                }
                if (s.TimeDomainPoints.Count > i)
                {
                    var pair = s.TimeDomainPoints.ElementAt(i);
                    cw.WriteField(pair.Key);
                    cw.WriteField(pair.Value);
                }
                else
                {
                    cw.WriteField(string.Empty);
                    cw.WriteField(string.Empty);
                }
                if (s.TimeDiscrepancyPoints.Count > i)
                {
                    var pair = s.TimeDiscrepancyPoints.ElementAt(i);
                    cw.WriteField(pair.Key);
                    cw.WriteField(pair.Value);
                }
                else
                {
                    cw.WriteField(string.Empty);
                    cw.WriteField(string.Empty);
                }
                await cw.NextRecordAsync();
            }
        }


        protected TextWriter? _BackupElectrometerWriter;
        protected CsvWriter? _BackupElectrometerCsvWriter;
        protected TextWriter? _BackupPositionWriter;
        protected CsvWriter? _BackupPositionCsvWriter;
        protected string _FolderPath;

        protected async Task CreateBackupFile(TextWriter? backupWriter, CsvWriter? backupCsvWriter, string suffix)
        {
            if (backupCsvWriter != null) await backupCsvWriter.FlushAsync();
            backupWriter?.Close();
            backupWriter = new StreamWriter(Path.Combine(_FolderPath, BackupSubfolderName, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{suffix}.csv"));
            try
            {
                backupCsvWriter?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                
            }
            backupCsvWriter = new CsvWriter(backupWriter, CultureInfo.InvariantCulture);
            await backupCsvWriter.NextRecordAsync();
        }
        protected static async Task LogPointBackup(CsvWriter? csvWriter, Func<Task> fallbackCreationAction, TimestampedResult r)
        {
            if (csvWriter == null) await fallbackCreationAction();
            csvWriter!.WriteField($"{r.Timestamp:yyyy-MM-dd HH-mm-ss}");
            csvWriter!.WriteField(r.Result);
            await csvWriter!.NextRecordAsync();
        }
    }
}