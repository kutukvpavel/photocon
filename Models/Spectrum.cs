using System;
using System.Collections.Generic;
using System.Linq;

namespace photocon.Models
{
    public class Spectrum
    {
        public enum DataChange
        {
            PointAdded,
            Cleared
        }
        public class DataChangedEventArgs : EventArgs
        {
            public DataChangedEventArgs(DataChange type)
            {
                ChangeType = type;
            }

            public DataChange ChangeType { get; }
            public KeyValuePair<double, double>? PositionDomain { get; set; }
            public KeyValuePair<DateTime, double>? TimeDomain { get; set; }
            public KeyValuePair<DateTime, double>? TimeDiscrepancy { get; set; }
        }

        public static double XyTimeDiscrepancyLimitSeconds { get; set; } = 0.4;

        protected TimestampedResult? _LastX = null;
        protected double _DiscrepancyAccumulator = 0;

        public event EventHandler<DataChangedEventArgs>? DataChanged;

        public Spectrum(ScanParams acqParams)
        {
            AcquisitionParameters = acqParams;
        }

        public object LockObject = new();
        public SortedDictionary<double, double> PositionDomainPoints { get; } = new();
        public SortedDictionary<DateTime, double> TimeDomainPoints { get; } = new();
        public SortedDictionary<DateTime, double> TimeDiscrepancyPoints { get; } = new();
        public double MaxTimeDiscrepancySeconds { get; private set; } = 0;
        public double AverageTimeDiscrepancySeconds { get; private set; } = double.NaN;
        public bool IsEmpty => TimeDiscrepancyPoints.Count == 0;
        public int MaxLength => Math.Max(Math.Max(PositionDomainPoints.Count, TimeDomainPoints.Count), TimeDiscrepancyPoints.Count);
        public bool EnablePositionalDomain { get; set; } = true;
        public bool EnableTimeDomain { get; set; } = true;
        public bool EnableTimeDiscrepancy { get; set; } = true;
        public ScanParams AcquisitionParameters { get; private set; }

        public void SetAcquisitionParameters(ScanParams p)
        {
            AcquisitionParameters = new(p);
        }
        public void UpdateX(TimestampedResult x)
        {
            lock (LockObject)
            {
                _LastX = x;
            }
        }
        public void AddY(TimestampedResult y)
        {
            KeyValuePair<double, double>? positional = null;
            KeyValuePair<DateTime, double>? time = null;
            KeyValuePair<DateTime, double>? discr = null;
            lock (LockObject)
            {
                if (!_LastX.HasValue) return;
                double discrepancy = (y.Timestamp - _LastX.Value.Timestamp).TotalSeconds;
                discr = new KeyValuePair<DateTime, double>(y.Timestamp, discrepancy);
                TimeDiscrepancyPoints.Add(y.Timestamp, discrepancy);
                if (MaxTimeDiscrepancySeconds < discrepancy) MaxTimeDiscrepancySeconds = discrepancy;
                _DiscrepancyAccumulator += discrepancy;
                AverageTimeDiscrepancySeconds = _DiscrepancyAccumulator / TimeDiscrepancyPoints.Count;
                if (discrepancy > XyTimeDiscrepancyLimitSeconds)
                {
                    Program.LogExceptionWithMessage(new InvalidOperationException(), "X-Y time difference too large, skipping point");
                    DataChanged?.Invoke(this, new DataChangedEventArgs(DataChange.PointAdded) { TimeDiscrepancy = discr });
                    return;
                }
                positional = new(_LastX.Value.Result, y.Result);
                PositionDomainPoints.Add(positional.Value.Key, positional.Value.Value);
                time = new(y.Timestamp, y.Result);
                TimeDomainPoints.Add(time.Value.Key, time.Value.Value);
                _LastX = null;
            }
            DataChanged?.Invoke(this, new DataChangedEventArgs(DataChange.PointAdded) 
            {
                TimeDiscrepancy = discr,
                TimeDomain = time,
                PositionDomain = positional
            });
        }
        
        public void Clear()
        {
            TimeDiscrepancyPoints.Clear();
            TimeDomainPoints.Clear();
            PositionDomainPoints.Clear();
            AverageTimeDiscrepancySeconds = double.NaN;
            MaxTimeDiscrepancySeconds = 0;
            _LastX = null;
            _DiscrepancyAccumulator = 0;
            DataChanged?.Invoke(this, new DataChangedEventArgs(DataChange.Cleared));
        }

        public Tuple<double, double> GetPositionAxisLimits()
        {
            return new Tuple<double, double>(Math.Min(AcquisitionParameters.Start, AcquisitionParameters.End),
                Math.Max(AcquisitionParameters.Start, AcquisitionParameters.End));
        }
    }
}