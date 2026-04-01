using System;

namespace photocon.Models
{
    public struct TimestampedResult
    {
        public TimestampedResult(double result)
        {
            Timestamp = DateTime.UtcNow;
            Result = result;
        }

        DateTime Timestamp;
        double Result;
    }
}