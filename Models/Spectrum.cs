using System;
using System.Collections.Generic;

namespace photocon.Models
{
    public class Spectrum
    {
        public static double XyTimeDiscrepancyLimitSeconds { get; set; } = 0.4;

        protected TimestampedResult? _LastX = null;
        protected double _DiscrepancyAccumulator = 0;

        public Spectrum()
        {
            
        }

        public object LockObject = new();
        public SortedDictionary<double, double> PositionDomainPoints { get; } = new();
        public SortedDictionary<DateTime, double> TimeDomainPoints { get; } = new();
        public SortedDictionary<DateTime, double> TimeDiscrepancyPoints { get; } = new();
        public double MaxTimeDiscrepancySeconds { get; private set; } = 0;
        public double AverageTimeDiscrepancySeconds { get; private set; } = double.NaN;
        public bool IsEmpty => TimeDiscrepancyPoints.Count == 0;
        public int MaxLength => Math.Max(Math.Max(PositionDomainPoints.Count, TimeDomainPoints.Count), TimeDiscrepancyPoints.Count);

        public void UpdateX(TimestampedResult x)
        {
            lock (LockObject)
            {
                _LastX = x;
            }
        }
        public void AddY(TimestampedResult y)
        {
            lock (LockObject)
            {
                if (!_LastX.HasValue) return;
                double discrepancy = (y.Timestamp - _LastX.Value.Timestamp).TotalSeconds;
                TimeDiscrepancyPoints.Add(y.Timestamp, discrepancy);
                if (MaxTimeDiscrepancySeconds < discrepancy) MaxTimeDiscrepancySeconds = discrepancy;
                _DiscrepancyAccumulator += discrepancy;
                AverageTimeDiscrepancySeconds = _DiscrepancyAccumulator / TimeDiscrepancyPoints.Count;
                if (discrepancy > XyTimeDiscrepancyLimitSeconds)
                {
                    Program.LogExceptionWithMessage(new InvalidOperationException(), "X-Y time difference too large, skipping point");
                    return;
                }
                PositionDomainPoints.Add(_LastX.Value.Result, y.Result);
                TimeDomainPoints.Add(y.Timestamp, y.Result);
                _LastX = null;
            }
        }
    }
}