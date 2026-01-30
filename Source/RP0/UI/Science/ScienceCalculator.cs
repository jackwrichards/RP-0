using System;
using System.Collections.Generic;
using System.Linq;

namespace RP0.UI.Science
{
    /// <summary>
    /// Handles all science calculations and data aggregation
    /// </summary>
    public class ScienceCalculator
    {
        private static ScienceCalculator _instance;
        public static ScienceCalculator Instance => _instance ?? (_instance = new ScienceCalculator());

        private ScienceCalculator()
        {
        }

        /// <summary>
        /// Generate a complete science snapshot for the current game state
        /// </summary>
        public ScienceSnapshot GenerateSnapshot()
        {
            var snapshot = new ScienceSnapshot
            {
                Timestamp = Planetarium.GetUniversalTime()
            };

            // Get current science points
            snapshot.CurrentScience = ResearchAndDevelopment.Instance?.Science ?? 0;

            // Get total science collected (from career log if available)
            snapshot.TotalScienceCollected = CalculateTotalScienceCollected();

            // Get research efficiency bonuses
            snapshot.ScienceResearchEfficiency = GetScienceResearchEfficiency();
            snapshot.TechResearchEfficiency = GetTechResearchEfficiency();

            return snapshot;
        }

        /// <summary>
        /// Calculate total science ever collected
        /// </summary>
        private double CalculateTotalScienceCollected()
        {
            // Try to get from career log first
            if (CareerLog.Instance != null && CareerLog.Instance.IsEnabled)
            {
                var periods = CareerLog.Instance.GetRecentPeriods(int.MaxValue).ToList();
                if (periods.Count > 0)
                {
                    // Get the most recent period's cumulative science
                    var latestPeriod = periods.OrderByDescending(p => p.StartUT).FirstOrDefault();
                    if (latestPeriod != null)
                    {
                        return latestPeriod.ScienceEarned;
                    }
                }
            }

            // Fallback to current science if no career log
            return ResearchAndDevelopment.Instance?.Science ?? 0;
        }

        /// <summary>
        /// Get science research efficiency bonus (placeholder - needs actual implementation)
        /// </summary>
        private double GetScienceResearchEfficiency()
        {
            // TODO: Implement actual science research efficiency calculation
            // This might come from facility upgrades, programs, or other game mechanics
            return 1.0; // 100% = no bonus
        }

        /// <summary>
        /// Get tech research efficiency bonus (placeholder - needs actual implementation)
        /// </summary>
        private double GetTechResearchEfficiency()
        {
            // TODO: Implement actual tech research efficiency calculation
            // This might come from facility upgrades, programs, or other game mechanics
            return 1.0; // 100% = no bonus
        }

        /// <summary>
        /// Format science value with appropriate precision
        /// </summary>
        public static string FormatScience(double amount, string format = "N1")
        {
            return amount.ToString(format);
        }

        /// <summary>
        /// Get historical science data from career log
        /// </summary>
        public static System.Collections.Generic.List<(double UT, double Science)> GetHistoricalScienceData(int periodsToShow = 24)
        {
            var result = new System.Collections.Generic.List<(double, double)>();
            
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
                return result;

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow);
            foreach (var period in periods)
            {
                result.Add((period.StartUT, period.ScienceEarned));
            }

            return result;
        }
    }
}
