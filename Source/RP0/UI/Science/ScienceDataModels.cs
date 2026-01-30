using System;
using System.Collections.Generic;

namespace RP0.UI.Science
{
    /// <summary>
    /// Represents a complete science snapshot
    /// </summary>
    public class ScienceSnapshot
    {
        public double Timestamp { get; set; }
        public double CurrentScience { get; set; }
        public double TotalScienceCollected { get; set; }
        public double ScienceResearchEfficiency { get; set; }
        public double TechResearchEfficiency { get; set; }

        public ScienceSnapshot()
        {
            Timestamp = Planetarium.GetUniversalTime();
        }
    }
}
