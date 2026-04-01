using System.Globalization;
using System.Linq;

namespace photocon.Grbl
{
    public enum ResponseTypes
    {
        StatusReport,
        Alarm,
        OK,
        Error,
        Other,
        Unknown
    }
    public enum States
    {
        Home,
        Idle,
        Run,
        Other,
        Unknown
    }
    public readonly struct StatusReport
    {
        public StatusReport(string state, string position)
        {
            State = state switch
            {
                "Idle" => States.Idle,
                "Run" => States.Run,
                "Home" => States.Home,
                _ => States.Other
            };
            Position = float.Parse(position, CultureInfo.InvariantCulture);
        }

        public States State { get; }
        public float Position { get; }
    }
    public static class Parser
    {
        public static StatusReport ParseStatusReport(string report)
        {
            string[] splt = report.Trim('<', '>').Split('|', 2);
            string[] pos = splt[1].Split(':')[1].Split(',', 1);
            return new StatusReport(splt[0], pos[0]);
        }

        public static ResponseTypes GetResponseType(string report)
        {
            switch (report[0])
            {
                case '<':
                    if (report[^1] == '>') return ResponseTypes.StatusReport;
                    else goto default;
                case 'A':
                    if (report.StartsWith("ALARM")) return ResponseTypes.Alarm;
                    else goto default;
                case 'o':
                    if (report[1] == 'k') return ResponseTypes.OK;
                    else goto default;
                case 'e':
                    if (report.StartsWith("error")) return ResponseTypes.Error;
                    else goto default;
                case '>':
                case '[':
                case 'G':
                case 'S':
                    return ResponseTypes.Other;
                default: return ResponseTypes.Unknown;
            }
        }
    }
}