using YamlDotNet.Serialization;

namespace photocon.Models
{
    public class ScanParams
    {        
        public ScanParams()
        {
            
        }
        public ScanParams(ScanParams p)
        {
            Speed = p.Speed;
            Start = p.Start;
            End = p.End;
        }

        public float Speed { get; set; } = 0.2f; // nm/min
        public float Start { get; set; } = 500; // nm
        public float End { get; set; } = 200; // nm

        [YamlIgnore]
        public bool IsBacklashCorrectionRequired => Start > End;
        [YamlIgnore]
        public float BacklashCorrection { get; set; } = 0; // nm
    }
}