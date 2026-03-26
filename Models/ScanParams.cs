namespace photocon.Models
{
    public class ScanParams
    {
        public ScanParams()
        {
            
        }

        public float Speed { get; set; } = 1; // nm/min
        public float Start { get; set; } = 400; // nm
        public float End { get; set; } = 200; // nm
    }
}