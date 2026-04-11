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

        public async Task CreateNewBackupFile()
        {
            if (_BackupCsvWriter != null) await _BackupCsvWriter.FlushAsync();
            _BackupWriter?.Close();
            _BackupWriter = new StreamWriter(Path.Combine(_FolderPath, BackupSubfolderName, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"));
            try
            {
                _BackupCsvWriter?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                
            }
            _BackupCsvWriter = new CsvWriter(_BackupWriter, CultureInfo.InvariantCulture);
            await _BackupCsvWriter.NextRecordAsync();
        }

        public async Task LogPointBackup(double wavelength, double conductance)
        {
            if (_BackupCsvWriter == null) await CreateNewBackupFile();
            _BackupCsvWriter!.WriteField(wavelength);
            _BackupCsvWriter!.WriteField(conductance);
            await _BackupCsvWriter!.NextRecordAsync();
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


        protected TextWriter? _BackupWriter;
        protected CsvWriter? _BackupCsvWriter;
        protected string _FolderPath;
    }
}