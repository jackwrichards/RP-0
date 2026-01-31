using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KERBALISM;

namespace RP0.UI.Science
{
    /// <summary>
    /// Science tab UI with metrics and simple science grid
    /// </summary>
    public class ScienceGUI : UIBase
    {
        private ScienceSnapshot _currentSnapshot;
        private Vector2 _gridScrollPosition = Vector2.zero;
        private int _chartMonthsToShow = 120; // Default: 10 years (120 months)

        protected override void OnStart()
        {
            base.OnStart();
            SharedUIComponents.InitializeStyles();
        }

        /// <summary>
        /// Refresh all science calculations - called automatically on every render
        /// </summary>
        private void RefreshScienceData()
        {
            if (HighLogic.CurrentGame?.Mode != Game.Modes.CAREER)
                return;

            _currentSnapshot = ScienceCalculator.Instance.GenerateSnapshot();
        }

        /// <summary>
        /// Main science tab rendering
        /// </summary>
        public void RenderScienceTab()
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                GUILayout.Label("Science system not available", BoldLabel);
                return;
            }

            // Always refresh data to keep it live
            RefreshScienceData();

            // Top metrics row with chart selector on the same row
            RenderMetricsRow();

            GUILayout.Space(8);

            // Simple science grid
            RenderScienceGrid();

            GUILayout.Space(8);

            // Historical charts side by side
            if (CareerLog.Instance != null && CareerLog.Instance.IsEnabled)
            {
                GUILayout.BeginHorizontal();
                
                // Science chart
                GUILayout.BeginVertical();
                SharedUIComponents.BeginCard("Total Science Earned (Monthly)");
                RP0.UI.Budget.BudgetChartRenderer.RenderHistoricalScienceChart(_chartMonthsToShow, 280, 100);
                SharedUIComponents.EndCard();
                GUILayout.EndVertical();

                GUILayout.Space(8);

                // Researchers chart
                GUILayout.BeginVertical();
                SharedUIComponents.BeginCard("Researchers (Monthly)");
                RP0.UI.Budget.BudgetChartRenderer.RenderHistoricalResearchersChart(_chartMonthsToShow, 280, 100);
                SharedUIComponents.EndCard();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Render top-level metrics cards with chart selector on the right
        /// </summary>
        private void RenderMetricsRow()
        {
            GUILayout.BeginHorizontal();

            // Current Science
            SharedUIComponents.RenderMetricCard(
                "Current Science",
                $"{ScienceCalculator.FormatScience(_currentSnapshot.CurrentScience, "N1")} 🔬",
                SharedUIComponents.Colors.Accent,
                KSPUtil.PrintDate(_currentSnapshot.Timestamp, false)
            );

            GUILayout.Space(4);

            // Total Science Collected with milestone tooltip and subtitle
            string scienceMilestoneTooltip = "Key Milestones:\n" +
                "• 41 science - Unlocks Orbital Rocketry node\n" +
                "  (Your first major goal! Enables orbital launches)\n\n" +
                "• 62 science - Unlocks Satellite Era node\n" +
                "  (Enables advanced orbital experiments)";
            
            SharedUIComponents.RenderMetricCard(
                "Total Collected",
                ScienceCalculator.FormatScience(_currentSnapshot.TotalScienceCollected, "N1"),
                new Color(0.3f, 0.8f, 0.6f), // Teal
                "All-time science earned", // Subtitle
                scienceMilestoneTooltip // Tooltip
            );

            GUILayout.Space(4);

            // Current Researchers
            int researcherCount = SpaceCenterManagement.Instance?.Researchers ?? 0;
            double researcherSalary = MaintenanceHandler.Instance?.ResearchSalaryPerDay ?? 0;
            double totalSalary = researcherSalary * 365.25; // Annual salary
            double salaryPerResearcher = researcherCount > 0 ? totalSalary / researcherCount : 0;
            
            SharedUIComponents.RenderMetricCard(
                "Researchers",
                $"{researcherCount}",
                new Color(0.4f, 0.7f, 0.9f), // Light blue
                researcherCount > 0 ? $"√{totalSalary:N0}/yr (√{salaryPerResearcher:N0} ea)" : "No researchers"
            );

            // Chart range selector on the right side of metrics row
            if (CareerLog.Instance != null && CareerLog.Instance.IsEnabled)
            {
                GUILayout.FlexibleSpace();
                
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                _chartMonthsToShow = SharedUIComponents.RenderChartRangeSelector(_chartMonthsToShow);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Render simple science grid
        /// </summary>
        private void RenderScienceGrid()
        {
            SharedUIComponents.BeginCard("");

            // Define experiments (columns) - with practical descriptions for tooltips
            var experiments = new[]
            {
                new { Id = "RP0telemetry1", Name = "Telem", FullName = "Telemetry Analysis", Desc = "Great early science! Get high atmo & low orbit with basic sounding rockets." },
                new { Id = "temperatureScan", Name = "Temp", FullName = "Temperature Scan", Desc = "Great early science! Sounding rockets in Flying High, and Space low" },
                new { Id = "barometerScan", Name = "Baro", FullName = "Barometer Scan", Desc = "Great early science! Sounding rockets in Flying High, and Space low" },
                new { Id = "RP0bioScan1", Name = "Bio", FullName = "Biological Sample", Desc = "Sounding rocket science. Plan for ~4 launches to complete flying high and space low." },
                new { Id = "RP0photos1", Name = "Photo1", FullName = "Photography 1", Desc = "Great early science! Get low orbit & high atmo easily. Try downrange for biomes." },
                new { Id = "RP0bioScan2", Name = "Adv Bio", FullName = "Advanced Biological Sample", Desc = "Requires orbit. Minimal science from sounding rockets." },
                new { Id = "RP0massSpec1", Name = "Mass", FullName = "Mass Spectrometry", Desc = "Possible with sounding rockets but slow." },
                new { Id = "RP0cosmicRay1", Name = "Cosmic", FullName = "Cosmic Ray Science", Desc = "Orbital science only." },
                new { Id = "RP0infraredRad1", Name = "Infrared", FullName = "Infrared Radiometer", Desc = "Orbital science only." },
                new { Id = "RP0photos2", Name = "Photo2", FullName = "Photography 2", Desc = "Orbital science only. You can't transmit this science" },
                new { Id = "RP0magScan1", Name = "Mag", FullName = "Magnetometer Scan", Desc = "Orbital science only." },
                new { Id = "micrometeoriteDetect", Name = "Micro", FullName = "Micrometeorite Detection", Desc = "Orbital science only." },
                new { Id = "RP0visibleImaging1", Name = "Visual", FullName = "Visible Imaging", Desc = "Orbital science only." },
                new { Id = "crewReport", Name = "Crew", FullName = "Crew Report", Desc = "Most crew science not shown here. See Kerbalism Science Archive for full crew experiments." },
                new { Id = "evaReport", Name = "EVA", FullName = "EVA Report", Desc = "Most crew science not shown here. See Kerbalism Science Archive for full crew experiments." },
            };

            // Define situations (rows) - full descriptive labels with altitude info
            // Ordered from bottom to top: Earth Atmo, Earth Low, Earth Orbit, Moon High, Moon Low
            var situations = new[]
            {
                new { Id = "@MoonInSpaceLow", Label = "Moon Low", SubLabel = "<150km" },
                new { Id = "@MoonInSpaceHigh", Label = "Moon High", SubLabel = "<Moon SOI" },
                new { Id = "@EarthInSpaceHigh", Label = "Earth High", SubLabel = ">35,786km" },
                new { Id = "@EarthInSpaceLow", Label = "Earth Low", SubLabel = ">140km" },
                new { Id = "@EarthFlyingHigh", Label = "Earth Atmo", SubLabel = ">50km" }
            };

            // Larger dimensions for bigger fonts and longer labels
            const float situationWidth = 160f;
            const float cellWidth = 40f;
            const float totalWidth = situationWidth + (cellWidth * 15);

            // Create styled header style - italic and angled, larger font
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.LowerLeft
            };

            // Create styled label style for situations (bold), larger font
            var situationStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // No scroll view - just render directly
            GUILayout.BeginVertical();

            // Render header row (experiments only, no "Situation" label) - taller for angled text
            Rect headerRowRect = GUILayoutUtility.GetRect(totalWidth, 45);
            
            // Draw header background
            if (Event.current.type == EventType.Repaint)
            {
                var headerBgTex = SharedUIComponents.MakeTex(2, 2, SharedUIComponents.Colors.HeaderBackground);
                GUI.DrawTexture(headerRowRect, headerBgTex);
            }

            // Draw title in top left corner with tooltip and hover effect
            Rect titleRect = new Rect(headerRowRect.x + 4, headerRowRect.y + 2, situationWidth - 8, 40);
            bool isTitleHovered = titleRect.Contains(Event.current.mousePosition);
            
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isTitleHovered ? 14 : 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = isTitleHovered ? Color.white : SharedUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            
            var titleContent = new GUIContent("Early Science Checklist", "Quick overview of early science experiments (pre-1960).\n\nFor a complete list of all experiments and biomes, see the Kerbalism Science Archive.");
            GUI.Label(titleRect, titleContent, titleStyle);

            // Draw header labels - start after situation column (no label for it)
            float currentX = headerRowRect.x + situationWidth;

            foreach (var exp in experiments)
            {
                // Check if this column is being hovered
                Rect columnRect = new Rect(currentX, headerRowRect.y, cellWidth, headerRowRect.height);
                bool isColumnHovered = columnRect.Contains(Event.current.mousePosition);
                
                // Create tooltip content with full name and description
                var tooltipContent = new GUIContent(exp.Name, $"{exp.FullName}\n{exp.Desc}");
                
                // Create header style with hover effect
                var currentHeaderStyle = new GUIStyle(headerStyle);
                if (isColumnHovered)
                {
                    currentHeaderStyle.normal.textColor = Color.white;
                    currentHeaderStyle.fontSize = 13; // Slightly larger when hovered
                }
                
                // Save the current matrix
                Matrix4x4 matrixBackup = GUI.matrix;
                
                // Calculate pivot point - shifted right and down for better alignment
                Vector2 pivot = new Vector2(currentX + cellWidth - 8, headerRowRect.y + headerRowRect.height + 4);
                
                // Rotate -45 degrees around the pivot
                GUIUtility.RotateAroundPivot(-45f, pivot);
                
                // Draw the rotated label with tooltip - shifted left and positioned lower
                GUI.Label(new Rect(currentX + cellWidth - 8, headerRowRect.y + headerRowRect.height - 24, 80, 20), tooltipContent, currentHeaderStyle);
                
                // Restore the matrix
                GUI.matrix = matrixBackup;
                
                currentX += cellWidth;
            }

            GUILayout.Space(2);

            // Render data rows with increased height for sub-labels
            int rowIndex = 0;
            int totalRows = situations.Length;
            foreach (var situation in situations)
            {
                Rect rowRect = GUILayoutUtility.GetRect(totalWidth, 28);
                bool isHovered = rowRect.Contains(Event.current.mousePosition);

                // Calculate gradient color for background: blue at bottom (last row) to black at top (first row)
                float gradientPosition = (float)rowIndex / (totalRows - 1); // 0.0 at top, 1.0 at bottom
                Color gradientBgColor = Color.Lerp(
                    new Color(0.1f, 0.1f, 0.1f, 0.5f), // Near black at top
                    new Color(0.2f, 0.35f, 0.55f, 0.5f), // Blue at bottom
                    gradientPosition
                );

                // Draw gradient background for the situation column only
                if (Event.current.type == EventType.Repaint)
                {
                    Rect situationBgRect = new Rect(rowRect.x, rowRect.y, situationWidth, rowRect.height);
                    var bgTex = SharedUIComponents.MakeTex(2, 2, gradientBgColor);
                    GUI.DrawTexture(situationBgRect, bgTex);
                }

                // Draw subtle hover background over entire row
                if (isHovered && Event.current.type == EventType.Repaint)
                {
                    var hoverTex = SharedUIComponents.MakeTex(2, 2, new Color(0.25f, 0.25f, 0.28f, 0.3f));
                    GUI.DrawTexture(rowRect, hoverTex);
                }

                // Draw situation label (bold) - white text
                currentX = rowRect.x + 4;
                var situationLabelStyle = new GUIStyle(situationStyle)
                {
                    fontSize = isHovered ? 14 : 13,
                    normal = { textColor = Color.white }
                };
                
                // Measure the main label width to position sub-label next to it
                GUIContent mainLabelContent = new GUIContent(situation.Label);
                Vector2 mainLabelSize = situationLabelStyle.CalcSize(mainLabelContent);
                
                // Draw main label
                GUI.Label(new Rect(currentX, rowRect.y, mainLabelSize.x, rowRect.height), situation.Label, situationLabelStyle);
                
                // Draw sub-label (altitude info) directly to the right of main label in smaller, slightly dimmer white text
                var subLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 0.9f) },
                    alignment = TextAnchor.MiddleLeft
                };
                GUI.Label(new Rect(currentX + mainLabelSize.x + 8, rowRect.y, situationWidth - mainLabelSize.x - 8, rowRect.height), situation.SubLabel, subLabelStyle);
                
                currentX += situationWidth;

                // Draw experiment data cells
                foreach (var exp in experiments)
                {
                    // Query Kerbalism for this experiment + situation (aggregated across all biomes)
                    var scienceData = GetKerbalismScienceData(exp.Id, situation.Id, "");
                    
                    // Add gold star if this is aggregated biome data
                    string displayText = scienceData.Max > 0
                        ? $"{scienceData.Collected:F0}/{scienceData.Max:F0}{(scienceData.IsBiomeAggregated ? "⭐" : "")}"
                        : "-";

                    // Color code based on completion - white-grey to light blue gradient
                    Color textColor = new Color(0.5f, 0.5f, 0.5f); // Grey for no data
                    if (scienceData.Max > 0)
                    {
                        float percent = (float)(scienceData.Collected / scienceData.Max);
                        // Gradient from white-grey (0%) to light blue (100%)
                        if (percent >= 0.99f)
                            textColor = new Color(0.55f, 0.75f, 0.85f); // Light blue when maxed
                        else if (percent >= 0.75f)
                            textColor = new Color(0.6f, 0.75f, 0.8f); // Medium-light blue
                        else if (percent >= 0.5f)
                            textColor = new Color(0.7f, 0.75f, 0.75f); // Light blue-grey
                        else if (percent >= 0.25f)
                            textColor = new Color(0.8f, 0.8f, 0.8f); // Very light grey
                        else
                            textColor = new Color(0.85f, 0.85f, 0.85f); // Almost white
                    }

                    var cellStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = isHovered ? 13 : 12,
                        normal = { textColor = textColor },
                        alignment = TextAnchor.MiddleCenter
                    };

                    GUI.Label(new Rect(currentX, rowRect.y, cellWidth, rowRect.height), displayText, cellStyle);
                    currentX += cellWidth;
                }

                // Draw subtle separator line
                GUILayout.Space(1);
                
                rowIndex++;
            }

            GUILayout.EndVertical();

            SharedUIComponents.EndCard();
        }


        /// <summary>
        /// Explicit mapping of which experiment+situation combinations are biome-specific
        /// </summary>
        private static readonly HashSet<string> BiomeSpecificCombinations = new HashSet<string>
        {
            "RP0visibleImaging1@EarthInSpaceLow",
            "RP0visibleImaging1@EarthFlyingHigh",
            "RP0visibleImaging1@MoonInSpaceLow",
            "RP0infraredRad1@EarthInSpaceHigh",
            "RP0infraredRad1@EarthInSpaceLow",
            "RP0infraredRad1@MoonInSpaceLow"
        };

        /// <summary>
        /// Get science data from Kerbalism for a specific experiment/situation
        /// Uses Kerbalism's API to check availability and aggregate biomes like the Science Archive does
        /// </summary>
        private (double Collected, double Max, bool IsBiomeAggregated) GetKerbalismScienceData(string experimentId, string situationId, string biome)
        {
            try
            {
                // Get the ExperimentInfo from Kerbalism's database
                ExperimentInfo expInfo = ScienceDB.GetExperimentInfo(experimentId);
                if (expInfo == null)
                    return (0, 0, false);

                // Get all subjects for this experiment from Kerbalism's database
                var bodiesSituationsBiomesSubject = ScienceDB.GetSubjectsForExperiment(expInfo);
                if (bodiesSituationsBiomesSubject == null)
                    return (0, 0, false);

                // Parse the situation ID to get body and situation
                // Format: "@EarthInSpaceLow" -> body="Earth", situation="InSpaceLow"
                string situationStr = situationId.TrimStart('@');
                
                // Find the body by checking which body name is at the start of the situation string
                CelestialBody targetBody = null;
                ScienceSituation scienceSituation = ScienceSituation.None;
                
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (situationStr.StartsWith(body.name))
                    {
                        targetBody = body;
                        string sitPart = situationStr.Substring(body.name.Length);
                        scienceSituation = ScienceSituationUtils.ScienceSituationDeserialize(sitPart);
                        break;
                    }
                }

                if (targetBody == null || scienceSituation == ScienceSituation.None)
                    return (0, 0, false);

                int bodyIndex = targetBody.flightGlobalsIndex;

                // Try to get the subjects for this body
                if (!bodiesSituationsBiomesSubject.TryGetValue(bodyIndex, out var situationsBiomesSubject))
                    return (0, 0, false);

                // Try to get the subjects for this situation
                if (!situationsBiomesSubject.TryGetValue(scienceSituation, out var biomesSubject))
                    return (0, 0, false);

                // Check if this is biome-specific by checking our explicit mapping
                string baseSubjectId = experimentId + situationId;
                bool isBiomeSpecific = BiomeSpecificCombinations.Contains(baseSubjectId);

                double totalCollected = 0;
                double totalMax = 0;
                int subjectCount = 0;

                // Iterate through all biomes for this situation
                foreach (var biomeEntry in biomesSubject)
                {
                    int biomeIndex = biomeEntry.Key;
                    List<SubjectData> subjectDataList = biomeEntry.Value;

                    foreach (SubjectData subjectData in subjectDataList)
                    {
                        if (subjectData != null)
                        {
                            totalCollected += subjectData.ScienceRetrievedInKSC;
                            totalMax += subjectData.ScienceMaxValue;
                            subjectCount++;
                        }
                    }
                }

                // If no subjects found, this combination is not available
                if (subjectCount == 0)
                    return (0, 0, false);

                // Return aggregated data with star if biome-specific
                return (totalCollected, totalMax, isBiomeSpecific);
            }
            catch (Exception ex)
            {
                RP0Debug.LogError($"Error querying Kerbalism: {ex.Message}");
                return (0, 0, false);
            }
        }
    }
}
