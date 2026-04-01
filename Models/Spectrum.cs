using System.Collections.Generic;

namespace photocon.Models
{
    public class Spectrum
    {
        public Spectrum(int capacity)
        {
            
        }

        public SortedDictionary<float, double> Points { get; } = new();

        
    }
}