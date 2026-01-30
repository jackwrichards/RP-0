using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RP0.UI.Budget
{
    /// <summary>
    /// Renders visual charts and graphs for budget data
    /// </summary>
    public static class BudgetChartRenderer
    {
        private const int ChartHeight = 120;
        private const int ChartWidth = 400;
        private const int MiniChartHeight = 60;
        private const int MiniChartWidth = 200;

        // Cached textures to prevent garbage collection
        private static Dictionary<Color, Texture2D> _cachedTextures = new Dictionary<Color, Texture2D>();


        /// <summary>
        /// Render a horizontal bar chart for budget categories
        /// </summary>
        public static void RenderCategoryBarChart(List<BudgetLineItem> items, float width = ChartWidth, float height = ChartHeight)
        {
            if (items == null || items.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No data to display", style);
                return;
            }

            // Calculate totals
            double maxAmount = items.Max(i => Math.Abs(i.GetTotal()));
            if (maxAmount == 0) maxAmount = 1;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float barHeight = (chartRect.height - (items.Count + 1) * 4) / items.Count;
            float currentY = chartRect.y + 4;

            foreach (var item in items)
            {
                double amount = Math.Abs(item.GetTotal());
                float barWidth = (float)(amount / maxAmount) * (chartRect.width - 120);

                // Draw bar background
                Rect barBgRect = new Rect(chartRect.x + 100, currentY, chartRect.width - 104, barHeight);
                DrawRect(barBgRect, new Color(0.2f, 0.2f, 0.2f));

                // Draw bar fill
                Rect barRect = new Rect(chartRect.x + 100, currentY, barWidth, barHeight);
                Color barColor = BudgetUIComponents.Colors.GetCategoryColor(item.Category);
                DrawRect(barRect, barColor);

                // Draw label
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                    alignment = TextAnchor.MiddleRight
                };
                Rect labelRect = new Rect(chartRect.x, currentY, 95, barHeight);
                GUI.Label(labelRect, item.Name, labelStyle);

                // Draw value
                var valueStyle = new GUIStyle(labelStyle)
                {
                    alignment = TextAnchor.MiddleLeft
                };
                Rect valueRect = new Rect(chartRect.x + 100 + barWidth + 5, currentY, 100, barHeight);
                GUI.Label(valueRect, BudgetCalculator.FormatCurrency(item.GetTotal(), "N0"), valueStyle);

                currentY += barHeight + 4;
            }
        }

        /// <summary>
        /// Render a donut/pie chart for expense breakdown
        /// </summary>
        public static void RenderExpenseDonutChart(List<BudgetLineItem> expenses, float size = 150)
        {
            if (expenses == null || expenses.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No expenses to display", style);
                return;
            }

            // Calculate category totals
            var categoryTotals = new Dictionary<BudgetCategory, double>();
            double total = 0;

            foreach (var item in expenses)
            {
                double amount = Math.Abs(item.GetTotal());
                if (!categoryTotals.ContainsKey(item.Category))
                    categoryTotals[item.Category] = 0;
                categoryTotals[item.Category] += amount;
                total += amount;
            }

            if (total == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No expenses", style);
                return;
            }

            Rect chartRect = GUILayoutUtility.GetRect(size, size);
            Vector2 center = new Vector2(chartRect.x + size / 2, chartRect.y + size / 2);
            float radius = size / 2 - 10;
            float innerRadius = radius * 0.6f;

            // Draw background circle
            DrawCircle(center, radius, new Color(0.15f, 0.15f, 0.18f));
            DrawCircle(center, innerRadius, new Color(0.1f, 0.1f, 0.12f));

            // Draw segments
            float currentAngle = -90; // Start at top
            foreach (var kvp in categoryTotals.OrderByDescending(k => k.Value))
            {
                float sweepAngle = (float)(kvp.Value / total * 360);
                Color segmentColor = BudgetUIComponents.Colors.GetCategoryColor(kvp.Key);
                
                DrawDonutSegment(center, innerRadius, radius, currentAngle, sweepAngle, segmentColor);
                
                currentAngle += sweepAngle;
            }

            // Draw center text (total)
            var centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleCenter
            };
            Rect centerRect = new Rect(center.x - 50, center.y - 10, 100, 20);
            GUI.Label(centerRect, BudgetCalculator.FormatCurrency(-total, "N0"), centerStyle);
        }

        /// <summary>
        /// Render a funds runway indicator (how many days until broke)
        /// </summary>
        public static void RenderFundsRunway(double currentFunds, double dailyCashFlow)
        {
            if (dailyCashFlow >= 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.Positive } };
                GUILayout.Label("Positive cash flow - funds increasing", style);
                return;
            }

            double daysRemaining = currentFunds / Math.Abs(dailyCashFlow);
            
            Color runwayColor;
            string icon;
            if (daysRemaining < 30)
            {
                runwayColor = BudgetUIComponents.Colors.Critical;
                icon = "!";
            }
            else if (daysRemaining < 90)
            {
                runwayColor = BudgetUIComponents.Colors.Warning;
                icon = "!";
            }
            else
            {
                runwayColor = BudgetUIComponents.Colors.Info;
                icon = "i";
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(25));
            
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = runwayColor },
                fontSize = 13
            };
            GUILayout.Label($"Funds runway: {daysRemaining:F0} days ({KSPUtil.PrintDateDeltaCompact(daysRemaining * 86400, false, false)})", labelStyle);
            
            GUILayout.EndHorizontal();

            // Progress bar showing runway
            float progress = Mathf.Clamp01((float)(daysRemaining / 365)); // 1 year = 100%
            BudgetUIComponents.RenderProgressBar(progress, null, runwayColor);
        }

        /// <summary>
        /// Render a historical bar chart showing income over time
        /// </summary>
        public static void RenderHistoricalIncomeChart(BudgetPeriod displayPeriod, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            // Adjust number of periods to show based on display period
            // Day view: show last 30 periods (months), Month view: show last 12 periods, Year view: show last 5 periods
            int periodsToShow = displayPeriod switch
            {
                BudgetPeriod.Day => 30,
                BudgetPeriod.Month => 12,
                BudgetPeriod.Year => 5,
                _ => 12
            };

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Calculate income for each period (subsidy + other income + vessel recovery + program funds)
            var incomeData = periods.Select(p => new
            {
                Period = p,
                TotalIncome = p.SubsidyPaidOut + p.OtherFundsEarned + p.VesselRecovery + p.ProgramFunds,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            double maxIncome = incomeData.Max(d => d.TotalIncome);
            if (maxIncome <= 0) maxIncome = 1;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float barWidth = (chartRect.width - 40) / incomeData.Count;
            float chartBottom = chartRect.y + chartRect.height - 5;
            float chartTop = chartRect.y + 10;
            float availableHeight = chartBottom - chartTop;

            // Draw bars
            for (int i = 0; i < incomeData.Count; i++)
            {
                var data = incomeData[i];
                float barHeight = (float)(data.TotalIncome / maxIncome) * availableHeight;
                float x = chartRect.x + 20 + (i * barWidth);
                
                // Draw bar with tooltip and hover effect
                Rect barRect = new Rect(x, chartBottom - barHeight, barWidth - 4, barHeight);
                Color barColor = BudgetUIComponents.Colors.Income;
                
                // Add tooltip and brighten on hover - use vertical slice for hover detection
                Rect sliceRect = new Rect(x, chartTop, barWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                if (isHovered)
                {
                    GUI.tooltip = $"{data.Label}\n√{data.TotalIncome:N0}";
                    barColor = new Color(barColor.r * 1.3f, barColor.g * 1.3f, barColor.b * 1.3f);
                }
                
                DrawRect(barRect, barColor);
            }

            // Draw title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };
            Rect titleRect = new Rect(chartRect.x + 5, chartRect.y + 2, chartRect.width - 10, 15);
            GUI.Label(titleRect, "Historical Income", titleStyle);
        }

        /// <summary>
        /// Render a historical stacked bar chart showing funds and unlock credit over time
        /// </summary>
        public static void RenderHistoricalFundsChart(int periodsToShow = 24, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Get funds and unlock credit data for each period
            var fundsData = periods.Select(p => new
            {
                Period = p,
                Funds = p.CurrentFunds,
                UnlockCredit = p.CurrentUnlockCredit,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            // Calculate the range considering both positive and negative values
            // We need to find the max of (Funds, UnlockCredit, Funds+UnlockCredit if both positive)
            // and min of (Funds, UnlockCredit, Funds+UnlockCredit if both negative)
            double maxValue = 0;
            double minValue = 0;
            
            foreach (var d in fundsData)
            {
                // Track individual maxes and mins
                maxValue = Math.Max(maxValue, d.Funds);
                maxValue = Math.Max(maxValue, d.UnlockCredit);
                minValue = Math.Min(minValue, d.Funds);
                minValue = Math.Min(minValue, d.UnlockCredit);
                
                // If both are positive, they stack upward
                if (d.Funds >= 0 && d.UnlockCredit >= 0)
                    maxValue = Math.Max(maxValue, d.Funds + d.UnlockCredit);
                // If both are negative, they stack downward
                else if (d.Funds < 0 && d.UnlockCredit < 0)
                    minValue = Math.Min(minValue, d.Funds + d.UnlockCredit);
            }
            
            // Add some padding to the range
            double range = maxValue - minValue;
            if (range == 0) range = Math.Max(Math.Max(Math.Abs(maxValue), Math.Abs(minValue)) * 0.1, 1);
            minValue = minValue - range * 0.1;
            maxValue = maxValue + range * 0.1;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float leftMargin = 55f;
            float barWidth = (chartRect.width - leftMargin - 5) / fundsData.Count;
            float chartBottom = chartRect.y + chartRect.height - 5; // No x-axis labels
            float chartTop = chartRect.y + 30; // More space for legend
            float availableHeight = chartBottom - chartTop;

            // Draw legend at top
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            float legendY = chartRect.y + 5;
            Color fundsColor = BudgetUIComponents.Colors.Accent; // Blue
            Color creditColor = new Color(1.0f, 0.9f, 0.3f); // Yellow
            
            Rect fundsLegendRect = new Rect(chartRect.x + 60, legendY, 100, 15);
            DrawRect(new Rect(fundsLegendRect.x - 20, fundsLegendRect.y + 3, 15, 10), fundsColor);
            GUI.Label(fundsLegendRect, "Funds", legendStyle);

            Rect creditLegendRect = new Rect(chartRect.x + 140, legendY, 120, 15);
            DrawRect(new Rect(creditLegendRect.x - 20, creditLegendRect.y + 3, 15, 10), creditColor);
            GUI.Label(creditLegendRect, "Unlock Credit", legendStyle);

            // Draw Y-axis labels
            var yAxisStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };
            
            // Draw max value at top
            Rect maxLabelRect = new Rect(chartRect.x, chartTop - 5, leftMargin - 5, 15);
            GUI.Label(maxLabelRect, $"√{maxValue:N0}", yAxisStyle);
            
            // Draw min value at bottom
            Rect minLabelRect = new Rect(chartRect.x, chartBottom - 10, leftMargin - 5, 15);
            GUI.Label(minLabelRect, $"√{minValue:N0}", yAxisStyle);
            
            // Draw mid value
            double midValue = (maxValue + minValue) / 2;
            float midY = chartTop + availableHeight / 2;
            Rect midLabelRect = new Rect(chartRect.x, midY - 7, leftMargin - 5, 15);
            GUI.Label(midLabelRect, $"√{midValue:N0}", yAxisStyle);

            // Calculate zero line position
            float zeroY = chartBottom - (float)((0 - minValue) / (maxValue - minValue)) * availableHeight;
            
            // Draw zero line if it's visible (thin white line)
            if (minValue < 0 && maxValue > 0)
            {
                Color zeroLineColor = new Color(1.0f, 1.0f, 1.0f, 0.4f); // Thin white with transparency
                DrawRect(new Rect(chartRect.x + leftMargin, zeroY, chartRect.width - leftMargin - 5, 1), zeroLineColor);
            }

            // Draw bars (stacked if same sign, separate if opposite signs)
            for (int i = 0; i < fundsData.Count; i++)
            {
                var data = fundsData[i];
                float x = chartRect.x + leftMargin + (i * barWidth);
                
                // Determine if we should stack or draw separately
                bool bothPositive = data.Funds >= 0 && data.UnlockCredit >= 0;
                bool bothNegative = data.Funds < 0 && data.UnlockCredit < 0;
                bool shouldStack = bothPositive || bothNegative;
                
                Rect fundsBarRect;
                Rect creditBarRect;
                
                if (shouldStack)
                {
                    // Stack the bars
                    if (bothPositive)
                    {
                        // Both positive: Funds on bottom, Credit on top
                        float fundsTop = zeroY - (float)((data.Funds - 0) / (maxValue - minValue)) * availableHeight;
                        float creditTop = zeroY - (float)((data.Funds + data.UnlockCredit - 0) / (maxValue - minValue)) * availableHeight;
                        
                        fundsBarRect = new Rect(x, fundsTop, barWidth - 4, zeroY - fundsTop);
                        creditBarRect = new Rect(x, creditTop, barWidth - 4, fundsTop - creditTop);
                    }
                    else
                    {
                        // Both negative: Funds on top, Credit on bottom (stacking downward)
                        float fundsBottom = zeroY + (float)((0 - data.Funds) / (maxValue - minValue)) * availableHeight;
                        float creditBottom = zeroY + (float)((0 - (data.Funds + data.UnlockCredit)) / (maxValue - minValue)) * availableHeight;
                        
                        fundsBarRect = new Rect(x, zeroY, barWidth - 4, fundsBottom - zeroY);
                        creditBarRect = new Rect(x, fundsBottom, barWidth - 4, creditBottom - fundsBottom);
                    }
                }
                else
                {
                    // Draw separately from zero line
                    if (data.Funds >= 0)
                    {
                        // Funds positive, credit negative
                        float fundsTop = zeroY - (float)((data.Funds - 0) / (maxValue - minValue)) * availableHeight;
                        float creditBottom = zeroY + (float)((0 - data.UnlockCredit) / (maxValue - minValue)) * availableHeight;
                        
                        fundsBarRect = new Rect(x, fundsTop, barWidth - 4, zeroY - fundsTop);
                        creditBarRect = new Rect(x, zeroY, barWidth - 4, creditBottom - zeroY);
                    }
                    else
                    {
                        // Funds negative, credit positive
                        float fundsBottom = zeroY + (float)((0 - data.Funds) / (maxValue - minValue)) * availableHeight;
                        float creditTop = zeroY - (float)((data.UnlockCredit - 0) / (maxValue - minValue)) * availableHeight;
                        
                        fundsBarRect = new Rect(x, zeroY, barWidth - 4, fundsBottom - zeroY);
                        creditBarRect = new Rect(x, creditTop, barWidth - 4, zeroY - creditTop);
                    }
                }
                
                // Use vertical slice for hover detection instead of individual bar segments
                Rect sliceRect = new Rect(x, chartTop, barWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                
                // Check if mouse is hovering over either segment for tooltip specificity
                bool isFundsHovered = isHovered && fundsBarRect.Contains(Event.current.mousePosition);
                bool isCreditHovered = isHovered && creditBarRect.Contains(Event.current.mousePosition);
                
                // Draw funds bar
                if (fundsBarRect.height > 0)
                {
                    Color fundsColorFinal = isHovered ? new Color(fundsColor.r * 1.3f, fundsColor.g * 1.3f, fundsColor.b * 1.3f) : fundsColor;
                    DrawRect(fundsBarRect, fundsColorFinal);
                }
                
                // Draw unlock credit bar
                if (creditBarRect.height > 0)
                {
                    Color creditColorFinal = isHovered ? new Color(creditColor.r * 1.3f, creditColor.g * 1.3f, creditColor.b * 1.3f) : creditColor;
                    DrawRect(creditBarRect, creditColorFinal);
                }
                
                // Set tooltip based on which segment is hovered, or show both if hovering in empty space
                if (isFundsHovered)
                {
                    GUI.tooltip = $"{data.Label}\nFunds: √{data.Funds:N0}";
                }
                else if (isCreditHovered)
                {
                    GUI.tooltip = $"{data.Label}\nUnlock Credit: √{data.UnlockCredit:N0}";
                }
                else if (isHovered)
                {
                    GUI.tooltip = $"{data.Label}\nFunds: √{data.Funds:N0}\nUnlock Credit: √{data.UnlockCredit:N0}";
                }
            }
        }

        /// <summary>
        /// Render a historical line chart showing confidence and reputation over time
        /// </summary>
        public static void RenderHistoricalConfidenceRepChart(int periodsToShow = 24, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Get data for each period
            var dataPoints = periods.Select(p => new
            {
                Period = p,
                Confidence = p.Confidence,
                Reputation = p.Reputation,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            double maxConfidence = dataPoints.Max(d => d.Confidence);
            double minConfidence = dataPoints.Min(d => d.Confidence);
            double maxReputation = dataPoints.Max(d => d.Reputation);
            double minReputation = dataPoints.Min(d => d.Reputation);
            
            double maxValue = Math.Max(maxConfidence, maxReputation);
            double minValue = Math.Min(minConfidence, minReputation);
            
            // Add 25% padding to range so lines don't feel crammed and you can see beyond current values
            double range = maxValue - minValue;
            if (range == 0) range = Math.Max(Math.Abs(maxValue) * 0.25, 10); // At least 25% range or 10
            minValue = minValue - range * 0.25;
            maxValue = maxValue + range * 0.25;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float leftMargin = 35f;
            float pointWidth = (chartRect.width - leftMargin - 10) / dataPoints.Count;
            float chartBottom = chartRect.y + chartRect.height - 5; // No x-axis labels
            float chartTop = chartRect.y + 30;
            float availableHeight = chartBottom - chartTop;

            // Draw Y-axis labels
            var yAxisStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };

            Rect maxLabelRect = new Rect(chartRect.x, chartTop - 5, leftMargin - 5, 15);
            GUI.Label(maxLabelRect, $"{maxValue:F0}", yAxisStyle);

            Rect minLabelRect = new Rect(chartRect.x, chartBottom - 10, leftMargin - 5, 15);
            GUI.Label(minLabelRect, $"{minValue:F0}", yAxisStyle);

            // Draw zero line if it's visible (thin white line)
            if (minValue < 0 && maxValue > 0)
            {
                float zeroY = chartBottom - (float)((0 - minValue) / (maxValue - minValue)) * availableHeight;
                Color zeroLineColor = new Color(1.0f, 1.0f, 1.0f, 0.4f); // Thin white with transparency
                DrawRect(new Rect(chartRect.x + leftMargin, zeroY, chartRect.width - leftMargin - 10, 1), zeroLineColor);
            }

            // Draw legend
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            float legendY = chartRect.y + 5;
            Rect confLegendRect = new Rect(chartRect.x + 60, legendY, 100, 15);
            var confColor = new Color(0.6f, 0.4f, 0.8f); // Purple
            DrawRect(new Rect(confLegendRect.x - 20, confLegendRect.y + 3, 15, 10), confColor);
            GUI.Label(confLegendRect, "Confidence", legendStyle);

            Rect repLegendRect = new Rect(chartRect.x + 160, legendY, 100, 15);
            var repColor = new Color(1.0f, 0.9f, 0.3f); // Yellow
            DrawRect(new Rect(repLegendRect.x - 20, repLegendRect.y + 3, 15, 10), repColor);
            GUI.Label(repLegendRect, "Reputation", legendStyle);

            // Event line colors - more saturated and visually appealing
            Color launchLineColor = new Color(0.2f, 1.0f, 0.3f, 0.25f); // Bright vibrant green for launches (more transparent)
            Color contractLineColor = new Color(1.0f, 1.0f, 1.0f, 0.25f); // Pure white for contracts (more transparent)
            
            // Add legend for event lines
            Rect launchLegendRect = new Rect(chartRect.x + 260, legendY, 100, 15);
            DrawRect(new Rect(launchLegendRect.x - 20, launchLegendRect.y + 3, 15, 10), launchLineColor);
            GUI.Label(launchLegendRect, "Launches", legendStyle);
            
            Rect contractLegendRect = new Rect(chartRect.x + 350, legendY, 100, 15);
            DrawRect(new Rect(contractLegendRect.x - 20, contractLegendRect.y + 3, 15, 10), contractLineColor);
            GUI.Label(contractLegendRect, "Contracts", legendStyle);

            // Draw vertical lines for launches and contract completions
            
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                var launchEvents = CareerLog.Instance?.GetLaunchEventsForPeriod(data.Period);
                var contractEvents = CareerLog.Instance?.GetContractEventsForPeriod(data.Period);
                
                // Count completed contracts (not accepted/failed/cancelled)
                int completedContracts = contractEvents?.Count(c => c.Type == ContractEventType.Complete) ?? 0;
                int launches = launchEvents?.Count ?? 0;
                
                float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                
                // Draw launch line if there were launches - thinner
                if (launches > 0)
                {
                    DrawLine(new Vector2(x, chartTop), new Vector2(x, chartBottom), launchLineColor, 1.5f);
                }
                
                // Draw contract line if there were completed contracts - thinner
                if (completedContracts > 0)
                {
                    DrawLine(new Vector2(x, chartTop), new Vector2(x, chartBottom), contractLineColor, 1.5f);
                }
            }

            // Draw lines connecting points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var data1 = dataPoints[i];
                var data2 = dataPoints[i + 1];

                float x1 = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                float x2 = chartRect.x + leftMargin + ((i + 1) * pointWidth) + pointWidth / 2;

                // Confidence line - thinner
                float confY1 = chartBottom - (float)((data1.Confidence - minValue) / (maxValue - minValue)) * availableHeight;
                float confY2 = chartBottom - (float)((data2.Confidence - minValue) / (maxValue - minValue)) * availableHeight;
                DrawLine(new Vector2(x1, confY1), new Vector2(x2, confY2), confColor, 1.25f);

                // Reputation line - thinner
                float repY1 = chartBottom - (float)((data1.Reputation - minValue) / (maxValue - minValue)) * availableHeight;
                float repY2 = chartBottom - (float)((data2.Reputation - minValue) / (maxValue - minValue)) * availableHeight;
                DrawLine(new Vector2(x1, repY1), new Vector2(x2, repY2), repColor, 1.25f);
            }

            // Draw points and labels
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;

                // Add tooltip for vertical slice and brighten on hover
                Rect sliceRect = new Rect(x - pointWidth / 2, chartTop, pointWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                
                if (isHovered)
                {
                    var launchEvents = CareerLog.Instance?.GetLaunchEventsForPeriod(data.Period);
                    var contractEvents = CareerLog.Instance?.GetContractEventsForPeriod(data.Period);
                    int completedContracts = contractEvents?.Count(c => c.Type == ContractEventType.Complete) ?? 0;
                    int launches = launchEvents?.Count ?? 0;
                    
                    string tooltip = $"{data.Label}\nConfidence: {data.Confidence:F1}\nReputation: {data.Reputation:F1}";
                    
                    if (launches > 0)
                    {
                        tooltip += $"\n\nLaunches: {launches}";
                        if (launches <= 5 && launchEvents != null)
                        {
                            foreach (var launch in launchEvents.Take(5))
                            {
                                tooltip += $"\n  • {launch.VesselName}";
                            }
                        }
                    }
                    
                    if (completedContracts > 0)
                    {
                        tooltip += $"\n\nContracts Completed: {completedContracts}";
                        var completed = contractEvents?.Where(c => c.Type == ContractEventType.Complete).Take(5).ToList();
                        if (completed != null && completed.Count <= 5)
                        {
                            foreach (var contract in completed)
                            {
                                tooltip += $"\n  • {contract.DisplayName}";
                            }
                        }
                    }
                    
                    GUI.tooltip = tooltip;
                }

                // Confidence point with hover effect - smaller dots
                float confY = chartBottom - (float)((data.Confidence - minValue) / (maxValue - minValue)) * availableHeight;
                Color confColorFinal = isHovered ? new Color(confColor.r * 1.4f, confColor.g * 1.4f, confColor.b * 1.4f) : confColor;
                DrawCircle(new Vector2(x, confY), isHovered ? 3.0f : 1.0f, confColorFinal, 8);

                // Reputation point with hover effect - smaller dots
                float repY = chartBottom - (float)((data.Reputation - minValue) / (maxValue - minValue)) * availableHeight;
                Color repColorFinal = isHovered ? new Color(repColor.r * 1.4f, repColor.g * 1.4f, repColor.b * 1.4f) : repColor;
                DrawCircle(new Vector2(x, repY), isHovered ? 3.0f : 1.0f, repColorFinal, 8);
            }
        }

        /// <summary>
        /// Render a historical line chart showing engineers and researchers count over time
        /// </summary>
        public static void RenderHistoricalPersonnelChart(int periodsToShow = 24, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Get data for each period
            var dataPoints = periods.Select(p => new
            {
                Period = p,
                Engineers = p.NumEngineers,
                Researchers = p.NumResearchers,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            double maxEngineers = dataPoints.Max(d => d.Engineers);
            double minEngineers = dataPoints.Min(d => d.Engineers);
            double maxResearchers = dataPoints.Max(d => d.Researchers);
            double minResearchers = dataPoints.Min(d => d.Researchers);
            
            double maxValue = Math.Max(maxEngineers, maxResearchers);
            double minValue = Math.Min(minEngineers, minResearchers);
            
            // Add 25% padding to range so lines don't feel crammed
            double range = maxValue - minValue;
            if (range == 0) range = Math.Max(Math.Abs(maxValue) * 0.25, 10);
            minValue = minValue - range * 0.25;
            maxValue = maxValue + range * 0.25;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float leftMargin = 35f;
            float pointWidth = (chartRect.width - leftMargin - 10) / dataPoints.Count;
            float chartBottom = chartRect.y + chartRect.height - 5;
            float chartTop = chartRect.y + 30;
            float availableHeight = chartBottom - chartTop;

            // Draw Y-axis labels
            var yAxisStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };

            Rect maxLabelRect = new Rect(chartRect.x, chartTop - 5, leftMargin - 5, 15);
            GUI.Label(maxLabelRect, $"{maxValue:F0}", yAxisStyle);

            Rect minLabelRect = new Rect(chartRect.x, chartBottom - 10, leftMargin - 5, 15);
            GUI.Label(minLabelRect, $"{minValue:F0}", yAxisStyle);

            // Draw legend
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            float legendY = chartRect.y + 5;
            Rect engLegendRect = new Rect(chartRect.x + 60, legendY, 100, 15);
            var engColor = new Color(0.85f, 0.75f, 0.55f); // Tan
            DrawRect(new Rect(engLegendRect.x - 20, engLegendRect.y + 3, 15, 10), engColor);
            GUI.Label(engLegendRect, "Engineers", legendStyle);

            Rect resLegendRect = new Rect(chartRect.x + 160, legendY, 100, 15);
            var resColor = BudgetUIComponents.Colors.Accent; // Blue
            DrawRect(new Rect(resLegendRect.x - 20, resLegendRect.y + 3, 15, 10), resColor);
            GUI.Label(resLegendRect, "Researchers", legendStyle);

            // Draw lines connecting points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var data1 = dataPoints[i];
                var data2 = dataPoints[i + 1];

                float x1 = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                float x2 = chartRect.x + leftMargin + ((i + 1) * pointWidth) + pointWidth / 2;

                // Engineers line
                float engY1 = chartBottom - (float)((data1.Engineers - minValue) / (maxValue - minValue)) * availableHeight;
                float engY2 = chartBottom - (float)((data2.Engineers - minValue) / (maxValue - minValue)) * availableHeight;
                DrawLine(new Vector2(x1, engY1), new Vector2(x2, engY2), engColor, 1.25f);

                // Researchers line
                float resY1 = chartBottom - (float)((data1.Researchers - minValue) / (maxValue - minValue)) * availableHeight;
                float resY2 = chartBottom - (float)((data2.Researchers - minValue) / (maxValue - minValue)) * availableHeight;
                DrawLine(new Vector2(x1, resY1), new Vector2(x2, resY2), resColor, 1.25f);
            }

            // Draw points
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;

                // Add tooltip for vertical slice
                Rect sliceRect = new Rect(x - pointWidth / 2, chartTop, pointWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                
                if (isHovered)
                {
                    GUI.tooltip = $"{data.Label}\nEngineers: {data.Engineers:F0}\nResearchers: {data.Researchers:F0}";
                }

                // Engineers point with hover effect
                float engY = chartBottom - (float)((data.Engineers - minValue) / (maxValue - minValue)) * availableHeight;
                Color engColorFinal = isHovered ? new Color(engColor.r * 1.4f, engColor.g * 1.4f, engColor.b * 1.4f) : engColor;
                DrawCircle(new Vector2(x, engY), isHovered ? 3.0f : 1.0f, engColorFinal, 8);

                // Researchers point with hover effect
                float resY = chartBottom - (float)((data.Researchers - minValue) / (maxValue - minValue)) * availableHeight;
                Color resColorFinal = isHovered ? new Color(resColor.r * 1.4f, resColor.g * 1.4f, resColor.b * 1.4f) : resColor;
                DrawCircle(new Vector2(x, resY), isHovered ? 3.0f : 1.0f, resColorFinal, 8);
            }
        }

        /// <summary>
        /// Render a historical line chart showing total science earned over time
        /// </summary>
        public static void RenderHistoricalScienceChart(int periodsToShow = 24, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Get data for each period
            var dataPoints = periods.Select(p => new
            {
                Period = p,
                Science = p.ScienceEarned,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            double maxScience = dataPoints.Max(d => d.Science);
            double minScience = dataPoints.Min(d => d.Science);
            
            // Add 25% padding to range so lines don't feel crammed and you can see beyond current values
            double range = maxScience - minScience;
            if (range == 0) range = Math.Max(Math.Abs(maxScience) * 0.25, 10); // At least 25% range or 10
            minScience = minScience - range * 0.25;
            maxScience = maxScience + range * 0.25;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float leftMargin = 35f;
            float pointWidth = (chartRect.width - leftMargin - 10) / dataPoints.Count;
            float chartBottom = chartRect.y + chartRect.height - 5; // No x-axis labels
            float chartTop = chartRect.y + 30;
            float availableHeight = chartBottom - chartTop;

            // Draw Y-axis labels
            var yAxisStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };

            Rect maxLabelRect = new Rect(chartRect.x, chartTop - 5, leftMargin - 5, 15);
            GUI.Label(maxLabelRect, $"{maxScience:F0}", yAxisStyle);

            Rect minLabelRect = new Rect(chartRect.x, chartBottom - 10, leftMargin - 5, 15);
            GUI.Label(minLabelRect, $"{minScience:F0}", yAxisStyle);

            // Draw zero line if it's visible (thin white line)
            if (minScience < 0 && maxScience > 0)
            {
                float zeroY = chartBottom - (float)((0 - minScience) / (maxScience - minScience)) * availableHeight;
                Color zeroLineColor = new Color(1.0f, 1.0f, 1.0f, 0.4f); // Thin white with transparency
                DrawRect(new Rect(chartRect.x + leftMargin, zeroY, chartRect.width - leftMargin - 10, 1), zeroLineColor);
            }

            // Draw legend
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            float legendY = chartRect.y + 5;
            Rect sciLegendRect = new Rect(chartRect.x + 60, legendY, 150, 15);
            var sciColor = BudgetUIComponents.Colors.Accent; // Blue
            DrawRect(new Rect(sciLegendRect.x - 20, sciLegendRect.y + 3, 15, 10), sciColor);
            GUI.Label(sciLegendRect, "Total Science Earned", legendStyle);
            
            // Add explanation for vertical lines
            var lineExplainStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleLeft
            };
            Rect lineExplainRect = new Rect(sciLegendRect.x + sciLegendRect.width + 5, legendY, 100, 15);
            GUI.Label(lineExplainRect, "(lines = techs)", lineExplainStyle);

            // Draw vertical lines for tech research events
            Color techLineColor = new Color(1.0f, 1.0f, 1.0f, 0.3f); // White with transparency
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                var techEvents = CareerLog.Instance?.GetTechEventsForPeriod(data.Period);
                if (techEvents != null && techEvents.Count > 0)
                {
                    float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                    DrawLine(new Vector2(x, chartTop), new Vector2(x, chartBottom), techLineColor, 1f);
                }
            }

            // Draw lines connecting points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var data1 = dataPoints[i];
                var data2 = dataPoints[i + 1];

                float x1 = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                float x2 = chartRect.x + leftMargin + ((i + 1) * pointWidth) + pointWidth / 2;

                // Science line - thinner
                float sciY1 = chartBottom - (float)((data1.Science - minScience) / (maxScience - minScience)) * availableHeight;
                float sciY2 = chartBottom - (float)((data2.Science - minScience) / (maxScience - minScience)) * availableHeight;
                DrawLine(new Vector2(x1, sciY1), new Vector2(x2, sciY2), sciColor, 1.25f);
            }

            // Draw points and labels
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;

                // Add tooltip for vertical slice and brighten on hover
                Rect sliceRect = new Rect(x - pointWidth / 2, chartTop, pointWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                
                if (isHovered)
                {
                    var techEvents = CareerLog.Instance?.GetTechEventsForPeriod(data.Period);
                    string tooltip = $"{data.Label}\nScience: {data.Science:F1}";
                    
                    if (techEvents != null && techEvents.Count > 0)
                    {
                        tooltip += "\n\nTechs Researched:";
                        foreach (var tech in techEvents)
                        {
                            tooltip += $"\n  • {tech.NodeName}";
                        }
                    }
                    
                    GUI.tooltip = tooltip;
                }

                // Science point with hover effect - smaller dots
                float sciY = chartBottom - (float)((data.Science - minScience) / (maxScience - minScience)) * availableHeight;
                Color sciColorFinal = isHovered ? new Color(sciColor.r * 1.4f, sciColor.g * 1.4f, sciColor.b * 1.4f) : sciColor;
                DrawCircle(new Vector2(x, sciY), isHovered ? 3.0f : 1.0f, sciColorFinal, 8);
            }
        }

        /// <summary>
        /// Render a historical line chart showing researcher count over time
        /// </summary>
        public static void RenderHistoricalResearchersChart(int periodsToShow = 24, float width = ChartWidth, float height = ChartHeight)
        {
            if (CareerLog.Instance == null || !CareerLog.Instance.IsEnabled)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("Career logging is not enabled", style);
                return;
            }

            var periods = CareerLog.Instance.GetRecentPeriods(periodsToShow).ToList();
            if (periods.Count == 0)
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = BudgetUIComponents.Colors.TextSecondary } };
                GUILayout.Label("No historical data available", style);
                return;
            }

            // Get data for each period
            var dataPoints = periods.Select(p => new
            {
                Period = p,
                Researchers = p.NumResearchers,
                Label = ROUtils.DTUtils.UTToDate(p.StartUT).ToString("MMM yy")
            }).ToList();

            double maxResearchers = dataPoints.Max(d => d.Researchers);
            double minResearchers = dataPoints.Min(d => d.Researchers);
            
            // Add 25% padding to range so lines don't feel crammed and you can see beyond current values
            double range = maxResearchers - minResearchers;
            if (range == 0) range = Math.Max(Math.Abs(maxResearchers) * 0.25, 10); // At least 25% range or 10
            minResearchers = minResearchers - range * 0.25;
            maxResearchers = maxResearchers + range * 0.25;

            Rect chartRect = GUILayoutUtility.GetRect(width, height);
            GUI.Box(chartRect, GUIContent.none);

            float leftMargin = 35f;
            float pointWidth = (chartRect.width - leftMargin - 10) / dataPoints.Count;
            float chartBottom = chartRect.y + chartRect.height - 5; // No x-axis labels
            float chartTop = chartRect.y + 30;
            float availableHeight = chartBottom - chartTop;

            // Draw Y-axis labels
            var yAxisStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };

            Rect maxLabelRect = new Rect(chartRect.x, chartTop - 5, leftMargin - 5, 15);
            GUI.Label(maxLabelRect, $"{maxResearchers:F0}", yAxisStyle);

            Rect minLabelRect = new Rect(chartRect.x, chartBottom - 10, leftMargin - 5, 15);
            GUI.Label(minLabelRect, $"{minResearchers:F0}", yAxisStyle);

            // Draw zero line if it's visible (thin white line)
            if (minResearchers < 0 && maxResearchers > 0)
            {
                float zeroY = chartBottom - (float)((0 - minResearchers) / (maxResearchers - minResearchers)) * availableHeight;
                Color zeroLineColor = new Color(1.0f, 1.0f, 1.0f, 0.4f); // Thin white with transparency
                DrawRect(new Rect(chartRect.x + leftMargin, zeroY, chartRect.width - leftMargin - 10, 1), zeroLineColor);
            }

            // Draw legend
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = BudgetUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            float legendY = chartRect.y + 5;
            Rect resLegendRect = new Rect(chartRect.x + 60, legendY, 150, 15);
            var resColor = new Color(0.4f, 0.7f, 0.9f); // Light blue
            DrawRect(new Rect(resLegendRect.x - 20, resLegendRect.y + 3, 15, 10), resColor);
            GUI.Label(resLegendRect, "Researchers", legendStyle);

            // Draw lines connecting points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var data1 = dataPoints[i];
                var data2 = dataPoints[i + 1];

                float x1 = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;
                float x2 = chartRect.x + leftMargin + ((i + 1) * pointWidth) + pointWidth / 2;

                // Researchers line - thinner
                float resY1 = chartBottom - (float)((data1.Researchers - minResearchers) / (maxResearchers - minResearchers)) * availableHeight;
                float resY2 = chartBottom - (float)((data2.Researchers - minResearchers) / (maxResearchers - minResearchers)) * availableHeight;
                DrawLine(new Vector2(x1, resY1), new Vector2(x2, resY2), resColor, 1.25f);
            }

            // Draw points and labels
            for (int i = 0; i < dataPoints.Count; i++)
            {
                var data = dataPoints[i];
                float x = chartRect.x + leftMargin + (i * pointWidth) + pointWidth / 2;

                // Add tooltip for vertical slice and brighten on hover
                Rect sliceRect = new Rect(x - pointWidth / 2, chartTop, pointWidth, chartBottom - chartTop);
                bool isHovered = sliceRect.Contains(Event.current.mousePosition);
                
                if (isHovered)
                {
                    GUI.tooltip = $"{data.Label}\nResearchers: {data.Researchers:F0}";
                }

                // Researchers point with hover effect - smaller dots
                float resY = chartBottom - (float)((data.Researchers - minResearchers) / (maxResearchers - minResearchers)) * availableHeight;
                Color resColorFinal = isHovered ? new Color(resColor.r * 1.4f, resColor.g * 1.4f, resColor.b * 1.4f) : resColor;
                DrawCircle(new Vector2(x, resY), isHovered ? 3.0f : 1.0f, resColorFinal, 8);
            }
        }


        // Helper drawing methods
        private static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;

            // Calculate the distance and angle
            float distance = Vector2.Distance(start, end);
            if (distance < 0.01f) return; // Skip very short lines
            
            Vector2 direction = (end - start) / distance;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Create a rect from start to end with the given thickness
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * thickness / 2;

            Vector2[] vertices = new Vector2[]
            {
                start + perpendicular,
                start - perpendicular,
                end - perpendicular,
                end + perpendicular
            };

            DrawQuad(vertices, color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;

            // Use cached texture to prevent garbage collection issues
            if (!_cachedTextures.TryGetValue(color, out Texture2D texture))
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, color);
                texture.Apply();
                _cachedTextures[color] = texture;
            }

            GUI.DrawTexture(rect, texture);
        }

        private static void DrawCircle(Vector2 center, float radius, Color color, int segments = 32)
        {
            if (Event.current.type != EventType.Repaint) return;

            Vector2[] points = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * 360 * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int i = 0; i < segments; i++)
            {
                DrawLine(points[i], points[(i + 1) % segments], color, 2f);
            }
        }

        private static void DrawDonutSegment(Vector2 center, float innerRadius, float outerRadius, float startAngle, float sweepAngle, Color color, int segments = 32)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (sweepAngle <= 0) return;

            int segmentCount = Mathf.Max(2, (int)(segments * (sweepAngle / 360)));
            
            for (int i = 0; i < segmentCount; i++)
            {
                float angle1 = (startAngle + (i / (float)segmentCount) * sweepAngle) * Mathf.Deg2Rad;
                float angle2 = (startAngle + ((i + 1) / (float)segmentCount) * sweepAngle) * Mathf.Deg2Rad;

                Vector2 outer1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * outerRadius;
                Vector2 outer2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * outerRadius;
                Vector2 inner1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * innerRadius;
                Vector2 inner2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * innerRadius;

                DrawQuad(new Vector2[] { outer1, outer2, inner2, inner1 }, color);
            }
        }

        private static void DrawQuad(Vector2[] vertices, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (vertices.Length != 4) return;

            // Use cached texture to prevent garbage collection issues
            if (!_cachedTextures.TryGetValue(color, out Texture2D texture))
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, color);
                texture.Apply();
                _cachedTextures[color] = texture;
            }

            // Unity IMGUI doesn't have direct quad drawing, so we approximate with GUI.DrawTexture
            // This is a simplified version - for production, consider using GL.Begin/End
            float minX = Mathf.Min(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x);
            float maxX = Mathf.Max(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x);
            float minY = Mathf.Min(vertices[0].y, vertices[1].y, vertices[2].y, vertices[3].y);
            float maxY = Mathf.Max(vertices[0].y, vertices[1].y, vertices[2].y, vertices[3].y);

            Rect rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            GUI.DrawTexture(rect, texture);
        }

    }
}
