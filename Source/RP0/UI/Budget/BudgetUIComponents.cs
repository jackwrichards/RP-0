using System;
using System.Collections.Generic;
using UnityEngine;

namespace RP0.UI.Budget
{
    /// <summary>
    /// Reusable UI components for modern budget display
    /// </summary>
    public static class BudgetUIComponents
    {
        // Color scheme for modern budget UI
        public static class Colors
        {
            public static readonly Color Income = new Color(0.2f, 0.8f, 0.4f);           // Green
            public static readonly Color Expense = new Color(0.9f, 0.3f, 0.3f);          // Red
            public static readonly Color Neutral = new Color(0.8f, 0.8f, 0.8f);          // Lighter Gray
            public static readonly Color Positive = new Color(0.3f, 0.9f, 0.5f);         // Bright Green
            public static readonly Color Negative = new Color(1.0f, 0.4f, 0.4f);         // Bright Red
            public static readonly Color Warning = new Color(1.0f, 0.7f, 0.2f);          // Orange
            public static readonly Color Critical = new Color(1.0f, 0.2f, 0.2f);         // Bright Red
            public static readonly Color Info = new Color(0.4f, 0.7f, 1.0f);             // Blue
            public static readonly Color CardBackground = new Color(0.15f, 0.15f, 0.18f); // Dark
            public static readonly Color CardBorder = new Color(0.3f, 0.3f, 0.35f);      // Medium Dark
            public static readonly Color HeaderBackground = new Color(0.1f, 0.1f, 0.12f); // Very Dark
            public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.95f);   // Almost White
            public static readonly Color TextSecondary = new Color(0.85f, 0.85f, 0.87f); // Brighter Light Gray
            public static readonly Color Accent = new Color(0.3f, 0.6f, 1.0f);           // Blue Accent

            public static Color GetCategoryColor(BudgetCategory category)
            {
                return category switch
                {
                    BudgetCategory.Facilities => new Color(0.6f, 0.4f, 0.8f),    // Purple
                    BudgetCategory.Personnel => new Color(0.4f, 0.7f, 0.9f),     // Light Blue
                    BudgetCategory.Operations => new Color(0.9f, 0.6f, 0.3f),    // Orange
                    BudgetCategory.Programs => new Color(0.3f, 0.8f, 0.6f),      // Teal
                    BudgetCategory.Income => Income,
                    _ => Neutral
                };
            }
        }

        // Cached GUIStyles
        private static GUIStyle _cardStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _valuePositiveStyle;
        private static GUIStyle _valueNegativeStyle;
        private static GUIStyle _valueNeutralStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _smallLabelStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _toggleButtonStyle;
        private static GUIStyle _alertCriticalStyle;
        private static GUIStyle _alertWarningStyle;
        private static GUIStyle _alertInfoStyle;
        private static GUIStyle _progressBarBackStyle;
        private static GUIStyle _progressBarFillStyle;

        // Cached textures to prevent garbage collection
        private static Dictionary<Color, Texture2D> _cachedTextures = new Dictionary<Color, Texture2D>();

        /// <summary>
        /// Initialize or refresh all styles
        /// </summary>
        public static void InitializeStyles()
        {
            // Card style - Slightly more padding for breathing room
            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.CardBackground) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Header style - Slightly more padding for breathing room
            _headerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.HeaderBackground), textColor = Colors.TextPrimary },
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(0, 0, 0, 2),
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            // Title style
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Subtitle style
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };

            // Value styles - Reduced font sizes
            _valuePositiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Colors.Positive },
                alignment = TextAnchor.MiddleRight
            };

            _valueNegativeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Colors.Negative },
                alignment = TextAnchor.MiddleRight
            };

            _valueNeutralStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Colors.TextPrimary },
                alignment = TextAnchor.MiddleRight
            };

            // Label style - Reduced font size
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Small label style - Reduced font size
            _smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };

            // Button style
            _buttonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                normal = { textColor = Colors.TextPrimary },
                hover = { textColor = Colors.Accent },
                fontSize = 13,
                padding = new RectOffset(12, 12, 6, 6)
            };

            // Toggle button style
            _toggleButtonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                normal = { textColor = Colors.TextSecondary },
                active = { textColor = Colors.Accent },
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4)
            };

            // Alert styles - Compact with minimal padding
            _alertCriticalStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(Colors.Critical.r, Colors.Critical.g, Colors.Critical.b, 0.2f)), textColor = Colors.Critical },
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(0, 0, 1, 1),
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _alertWarningStyle = new GUIStyle(_alertCriticalStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(Colors.Warning.r, Colors.Warning.g, Colors.Warning.b, 0.2f)), textColor = Colors.Warning }
            };

            _alertInfoStyle = new GUIStyle(_alertCriticalStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(Colors.Info.r, Colors.Info.g, Colors.Info.b, 0.2f)), textColor = Colors.Info },
                fontStyle = FontStyle.Normal
            };

            // Progress bar styles
            _progressBarBackStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f)) },
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            _progressBarFillStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.Accent) },
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        /// <summary>
        /// Create a solid color texture (cached to prevent GC issues)
        /// </summary>
        private static Texture2D MakeTex(int width, int height, Color col)
        {
            // Use cached texture if available
            if (_cachedTextures.TryGetValue(col, out Texture2D cached))
                return cached;

            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            
            // Cache the texture to prevent garbage collection
            _cachedTextures[col] = result;
            
            return result;
        }

        /// <summary>
        /// Render a modern card container
        /// </summary>
        public static void BeginCard(string title = null)
        {
            if (_cardStyle == null) InitializeStyles();

            GUILayout.BeginVertical(_cardStyle);
            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.Label(title, _headerStyle);
            }
        }

        public static void EndCard()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Render a budget line item with modern styling
        /// </summary>
        public static void RenderLineItem(BudgetLineItem item, int indentLevel = 0, bool showIcon = true)
        {
            if (_labelStyle == null) InitializeStyles();

            GUILayout.BeginHorizontal();

            // Indent
            if (indentLevel > 0)
                GUILayout.Space(indentLevel * 20);

            // Expand/collapse button for items with subitems
            if (item.HasSubItems)
            {
                string arrow = item.IsExpanded ? "▼" : "▶";
                if (GUILayout.Button(arrow, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    item.IsExpanded = !item.IsExpanded;
                }
            }
            else
            {
                GUILayout.Space(20);
            }

            // Icon
            if (showIcon && !string.IsNullOrEmpty(item.Icon))
            {
                GUILayout.Label(item.Icon, _labelStyle, GUILayout.Width(25));
            }

            // Name with tooltip
            var content = new GUIContent(item.Name, item.Description);
            GUILayout.Label(content, _labelStyle, GUILayout.ExpandWidth(true));

            // Amount
            double amount = item.Amount;
            GUIStyle valueStyle = amount > 0 ? _valuePositiveStyle : (amount < 0 ? _valueNegativeStyle : _valueNeutralStyle);
            GUILayout.Label(BudgetCalculator.FormatCurrency(amount), valueStyle, GUILayout.Width(100));

            // Click action
            if (item.OnClick != null && GUILayout.Button("→", GUILayout.Width(25)))
            {
                item.OnClick();
            }

            GUILayout.EndHorizontal();

            // Render subitems if expanded
            if (item.IsExpanded && item.HasSubItems)
            {
                foreach (var subItem in item.SubItems)
                {
                    RenderLineItem(subItem, indentLevel + 1, showIcon);
                }
            }
        }

        /// <summary>
        /// Render a budget line item permanently expanded without expand/collapse buttons
        /// </summary>
        public static void RenderLineItemExpanded(BudgetLineItem item, int indentLevel = 0, bool showIcon = true)
        {
            if (_labelStyle == null) InitializeStyles();

            GUILayout.BeginHorizontal();

            // Indent - Reduced spacing
            if (indentLevel > 0)
                GUILayout.Space(indentLevel * 12);

            // No expand/collapse button - reduced spacing
            GUILayout.Space(12);

            // Icon - Reduced width
            if (showIcon && !string.IsNullOrEmpty(item.Icon))
            {
                GUILayout.Label(item.Icon, _labelStyle, GUILayout.Width(18));
            }

            // Name with tooltip
            var content = new GUIContent(item.Name, item.Description);
            GUILayout.Label(content, _labelStyle, GUILayout.ExpandWidth(true));

            // Amount - Reduced width
            double amount = item.Amount;
            GUIStyle valueStyle = amount > 0 ? _valuePositiveStyle : (amount < 0 ? _valueNegativeStyle : _valueNeutralStyle);
            GUILayout.Label(BudgetCalculator.FormatCurrency(amount), valueStyle, GUILayout.Width(85));

            // Click action - Reduced width
            if (item.OnClick != null && GUILayout.Button("→", GUILayout.Width(20)))
            {
                item.OnClick();
            }

            GUILayout.EndHorizontal();

            // Always render subitems recursively
            if (item.HasSubItems)
            {
                foreach (var subItem in item.SubItems)
                {
                    RenderLineItemExpanded(subItem, indentLevel + 1, showIcon);
                }
            }
        }

        /// <summary>
        /// Render a summary metric card with fixed height
        /// </summary>
        public static void RenderMetricCard(string label, string value, Color valueColor, string subtitle = null, string tooltip = null)
        {
            if (_cardStyle == null) InitializeStyles();

            GUILayout.BeginVertical(_cardStyle, GUILayout.MinWidth(130), GUILayout.Height(60));
            
            var labelContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(label) : new GUIContent(label, tooltip);
            GUILayout.Label(labelContent, _subtitleStyle);
            
            var valueStyle = new GUIStyle(_titleStyle) { normal = { textColor = valueColor } };
            var valueContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(value) : new GUIContent(value, tooltip);
            GUILayout.Label(valueContent, valueStyle);
            
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(subtitle) : new GUIContent(subtitle, tooltip);
                GUILayout.Label(subtitleContent, _smallLabelStyle);
            }
            else
            {
                // Add spacing to maintain consistent height when no subtitle
                GUILayout.Label("", _smallLabelStyle);
            }
            
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Render an alert/notification with colored circle indicator
        /// </summary>
        public static void RenderAlert(BudgetAlert alert)
        {
            if (_alertInfoStyle == null) InitializeStyles();

            GUIStyle style = alert.Level switch
            {
                BudgetAlert.AlertLevel.Critical => _alertCriticalStyle,
                BudgetAlert.AlertLevel.Warning => _alertWarningStyle,
                _ => _alertInfoStyle
            };

            Color circleColor = alert.Level switch
            {
                BudgetAlert.AlertLevel.Critical => Colors.Critical,
                BudgetAlert.AlertLevel.Warning => Colors.Warning,
                _ => Colors.Info
            };

            // Fixed height for slim alerts
            GUILayout.BeginHorizontal(style, GUILayout.Height(24));
            
            // Left spacing for breathing room
            GUILayout.Space(8);
            
            // Draw colored circle indicator with vertical centering
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var circleStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, circleColor) }
            };
            GUILayout.Box(GUIContent.none, circleStyle, GUILayout.Width(10), GUILayout.Height(10));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Label with white text, left-aligned, vertically centered, bold and larger
            // Use BeginVertical to force left alignment
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var labelStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = Colors.TextPrimary }, // White text
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            GUILayout.Label(alert.Message, labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace(); // Push button to the right
            
            if (alert.OnClick != null)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("→", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    alert.OnClick();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Render a progress bar
        /// </summary>
        public static void RenderProgressBar(float progress, string label = null, Color? fillColor = null)
        {
            if (_progressBarBackStyle == null) InitializeStyles();

            progress = Mathf.Clamp01(progress);

            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.Label(label, _smallLabelStyle);
            }

            Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true), GUILayout.Height(18));
            
            // Background
            GUI.Box(rect, GUIContent.none, _progressBarBackStyle);
            
            // Fill
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            if (fillColor.HasValue)
            {
                var customFillStyle = new GUIStyle(_progressBarFillStyle)
                {
                    normal = { background = MakeTex(2, 2, fillColor.Value) }
                };
                GUI.Box(fillRect, GUIContent.none, customFillStyle);
            }
            else
            {
                GUI.Box(fillRect, GUIContent.none, _progressBarFillStyle);
            }

            // Percentage text
            var percentStyle = new GUIStyle(_smallLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(rect, $"{progress * 100:F0}%", percentStyle);
        }

        /// <summary>
        /// Render a period selector (Day/Month/Year)
        /// </summary>
        public static BudgetPeriod RenderPeriodSelector(BudgetPeriod currentPeriod)
        {
            if (_buttonStyle == null) InitializeStyles();

            GUILayout.BeginHorizontal();

            BudgetPeriod newPeriod = currentPeriod;

            // Create pressed button style that matches the top bar
            var pressedStyle = new GUIStyle(HighLogic.Skin.button);
            pressedStyle.normal = pressedStyle.active;

            if (GUILayout.Button("Day", currentPeriod == BudgetPeriod.Day ? pressedStyle : HighLogic.Skin.button))
                newPeriod = BudgetPeriod.Day;
            if (GUILayout.Button("Month", currentPeriod == BudgetPeriod.Month ? pressedStyle : HighLogic.Skin.button))
                newPeriod = BudgetPeriod.Month;
            if (GUILayout.Button("Year", currentPeriod == BudgetPeriod.Year ? pressedStyle : HighLogic.Skin.button))
                newPeriod = BudgetPeriod.Year;

            GUILayout.EndHorizontal();

            return newPeriod;
        }

        /// <summary>
        /// Render a compact budget summary with tooltip descriptions
        /// </summary>
        public static void RenderBudgetSummary(BudgetSnapshot snapshot)
        {
            if (_labelStyle == null) InitializeStyles();

            BeginCard();
            
            // Header with title and hint - make title bold
            GUILayout.BeginHorizontal(_headerStyle);
            var titleStyle = new GUIStyle(_headerStyle)
            {
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label("Budget Summary", titleStyle, GUILayout.ExpandWidth(false));
            GUILayout.Space(8);
            var hintStyle = new GUIStyle(_smallLabelStyle)
            {
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };
            GUILayout.Label("(hover for more info)", hintStyle);
            GUILayout.EndHorizontal();

            // Helper to render a compact budget line with tooltip and count info
            void RenderBudgetLine(string label, string tooltip, double amount, BudgetLineItem item = null, bool isIncome = false, bool showSeparator = true)
            {
                // Get rect for entire row to detect hover - reduced height from 20 to 16
                Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, _labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(16));
                bool isHovered = rowRect.Contains(Event.current.mousePosition);
                
                // Set tooltip for entire row
                if (isHovered)
                {
                    GUI.tooltip = tooltip;
                }
                
                // Draw label
                var labelContent = new GUIContent(label, tooltip);
                var labelStyleLarger = new GUIStyle(_labelStyle)
                {
                    fontSize = isHovered ? 14 : 12,
                    normal = { textColor = isHovered ? Color.white : _labelStyle.normal.textColor }
                };
                Rect labelRect = new Rect(rowRect.x, rowRect.y, 120, rowRect.height);
                GUI.Label(labelRect, labelContent, labelStyleLarger);
                
                // Draw count info in grey if available
                if (item != null && item.Count > 0 && !string.IsNullOrEmpty(item.CountLabel))
                {
                    var countStyle = new GUIStyle(_smallLabelStyle)
                    {
                        fontSize = isHovered ? 11 : 10,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                        alignment = TextAnchor.MiddleLeft
                    };
                    
                    string countText = $"{item.Count} {item.CountLabel}";
                    
                    // Add cost per unit if available, but not for engineers or scientists
                    if (item.CostPerUnit != 0 && item.CountLabel != "engineers" && item.CountLabel != "scientists")
                    {
                        countText += $" ({BudgetCalculator.FormatCurrency(item.CostPerUnit, "N0")} ea)";
                    }
                    
                    Rect countRect = new Rect(rowRect.x + 125, rowRect.y, 155, rowRect.height);
                    GUI.Label(countRect, countText, countStyle);
                }
                
                // Draw value
                GUIStyle valueStyle;
                if (isIncome)
                    valueStyle = new GUIStyle(_valuePositiveStyle) { fontSize = isHovered ? 14 : 12 };
                else if (amount < 0)
                    valueStyle = new GUIStyle(_valueNegativeStyle) { fontSize = isHovered ? 14 : 12 };
                else
                    valueStyle = new GUIStyle(_valueNeutralStyle) { fontSize = isHovered ? 14 : 12 };
                
                Rect valueRect = new Rect(rowRect.x + rowRect.width - 100, rowRect.y, 100, rowRect.height);
                GUI.Label(valueRect, BudgetCalculator.FormatCurrency(amount, "N0"), valueStyle);
                
                // Draw subtle horizontal separator line - exactly 1 pixel tall
                if (showSeparator)
                {
                    Rect separatorRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    if (Event.current.type == EventType.Repaint)
                    {
                        var separatorTex = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.28f, 0.5f));
                        GUI.DrawTexture(separatorRect, separatorTex);
                    }
                }
            }

            // Get individual line items
            var facilities = snapshot.Expenses.Find(e => e.Category == BudgetCategory.Facilities);
            var integrationTeams = snapshot.Expenses.Find(e => e.Name == "Integration Teams");
            var researchTeams = snapshot.Expenses.Find(e => e.Name == "Research Teams");
            var astronauts = snapshot.Expenses.Find(e => e.Name == "Astronaut Corps");
            var rollout = snapshot.Expenses.Find(e => e.Name == "Rollout & Airlaunch Prep");
            var construction = snapshot.Expenses.Find(e => e.Name == "Construction Projects");
            var subsidy = snapshot.Income.Find(e => e.Name == "Government Subsidy");
            var programs = snapshot.Income.FindAll(e => e.Category == BudgetCategory.Programs);

            // Calculate net after subsidy
            double subsidyAmount = subsidy?.GetTotal() ?? 0;
            double expensesBeforeSubsidy = (facilities?.GetTotal() ?? 0) +
                                           (integrationTeams?.GetTotal() ?? 0) +
                                           (researchTeams?.GetTotal() ?? 0) +
                                           (astronauts?.GetTotal() ?? 0);
            double netAfterSubsidy = expensesBeforeSubsidy + subsidyAmount;

            // Program budget (sum of all programs)
            double programBudget = 0;
            foreach (var program in programs)
            {
                programBudget += program.GetTotal();
            }

            // Helper to build detailed tooltip with sub-items
            string BuildDetailedTooltip(string description, BudgetLineItem item)
            {
                if (item == null || !item.HasSubItems)
                    return description;
                
                var tooltip = new System.Text.StringBuilder();
                tooltip.AppendLine(description);
                tooltip.AppendLine();
                tooltip.AppendLine("Breakdown:");
                
                foreach (var subItem in item.SubItems)
                {
                    tooltip.AppendLine($"  • {subItem.Name}: {BudgetCalculator.FormatCurrency(subItem.GetTotal(), "N0")}");
                    
                    // Add nested sub-items if they exist
                    if (subItem.HasSubItems)
                    {
                        foreach (var nestedItem in subItem.SubItems)
                        {
                            tooltip.AppendLine($"    - {nestedItem.Name}: {BudgetCalculator.FormatCurrency(nestedItem.GetTotal(), "N0")}");
                        }
                    }
                }
                
                return tooltip.ToString().TrimEnd();
            }
            
            // Render each line with separators and count info
            RenderBudgetLine("Facilities",
                facilities != null && facilities.HasSubItems
                    ? BuildDetailedTooltip("The cost to operate all the buildings, LC's, and pads.", facilities)
                    : "The cost to operate all the buildings, LC's, and pads.\n\nNo facilities data available.",
                facilities?.GetTotal() ?? 0, facilities);

            RenderBudgetLine("Integration Teams",
                integrationTeams != null && integrationTeams.HasSubItems
                    ? BuildDetailedTooltip("The cost to pay all of your engineers' salaries. They build rockets.", integrationTeams)
                    : "The cost to pay all of your engineers' salaries. They build rockets.\n\nNo integration teams data available.",
                integrationTeams?.GetTotal() ?? 0, integrationTeams);

            RenderBudgetLine("Research Teams",
                researchTeams != null && researchTeams.Amount != 0
                    ? "The cost to pay all of your researchers' salaries. They research new tech."
                    : "The cost to pay all of your researchers' salaries. They research new tech.\n\nNo research teams data available.",
                researchTeams?.GetTotal() ?? 0, researchTeams);

            RenderBudgetLine("Astronauts",
                astronauts != null && astronauts.HasSubItems
                    ? BuildDetailedTooltip("The cost to pay all of your astronauts' salaries. They plant flags on moons.", astronauts)
                    : "The cost to pay all of your astronauts' salaries. They plant flags on moons.\n\nNo astronaut data available.",
                astronauts?.GetTotal() ?? 0, astronauts);

            RenderBudgetLine("Avg. Subsidy",
                subsidy != null && subsidy.Amount != 0
                    ? "The expected amount of \"free\" funding you receive. Subsidy can be used to pay for the above costs, but any unused subsidy is lost."
                    : "The expected amount of \"free\" funding you receive. Subsidy can be used to pay for the above costs, but any unused subsidy is lost.\n\nNo subsidy data available.",
                subsidyAmount, null, true);

            RenderBudgetLine("Net (after subsidy)",
                "How much you owe for the above, after the subsidy.\n\nThis is: Facilities + Integration + Research + Astronauts + Subsidy",
                netAfterSubsidy, null);

            RenderBudgetLine("Rollout/Airlaunch",
                rollout != null && rollout.Amount != 0
                    ? "The cost to prepare rockets and aircraft for launch, only paid when actually preparing a given craft for flight."
                    : "The cost to prepare rockets and aircraft for launch, only paid when actually preparing a given craft for flight.\n\nNo active rollout/airlaunch operations.",
                rollout?.GetTotal() ?? 0, rollout);

            RenderBudgetLine("Constructions",
                construction != null && construction.HasSubItems
                    ? BuildDetailedTooltip("The cost to pay for any facility upgrades.", construction)
                    : "The cost to pay for any facility upgrades.\n\nNo active construction projects.",
                construction?.GetTotal() ?? 0, construction);

            string programTooltip;
            // Create a combined program item for count display
            BudgetLineItem programsItem = null;
            if (programs != null && programs.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("The amount of funding your accepted programs are paying you.");
                sb.AppendLine();
                sb.AppendLine("Active Programs:");
                foreach (var program in programs)
                {
                    sb.AppendLine($"  • {program.Name}: {BudgetCalculator.FormatCurrency(program.GetTotal(), "N0")}");
                }
                programTooltip = sb.ToString().TrimEnd();
                
                programsItem = new BudgetLineItem
                {
                    Count = programs.Count,
                    CountLabel = programs.Count == 1 ? "program" : "programs"
                };
            }
            else
            {
                programTooltip = "The amount of funding your accepted programs are paying you.\n\nNo active programs.";
            }
            
            RenderBudgetLine("Program Budget",
                programTooltip,
                programBudget, programsItem, true, false); // Programs are income (positive)

            // Separator line - reduced spacing
            GUILayout.Space(1);
            var separatorStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.CardBorder) }
            };
            GUILayout.Box(GUIContent.none, separatorStyle, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUILayout.Space(1);

            // Net Cash Flow with period indicator
            string periodText = snapshot.Period switch
            {
                BudgetPeriod.Day => "Daily",
                BudgetPeriod.Month => "Monthly",
                BudgetPeriod.Year => "Yearly",
                _ => ""
            };
            
            // Get rect for entire row to detect hover - reduced height from 20 to 16
            Rect netRowRect = GUILayoutUtility.GetRect(GUIContent.none, _labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(16));
            bool isNetHovered = netRowRect.Contains(Event.current.mousePosition);
            
            // Set tooltip for entire row
            if (isNetHovered)
            {
                GUI.tooltip = "How much your bank account goes up or down when everything is said and done.";
            }
            
            var labelContent = new GUIContent("Net Cash Flow", "How much your bank account goes up or down when everything is said and done.");
            
            // Change color and size on hover
            var labelStyleLarger = new GUIStyle(_labelStyle)
            {
                fontSize = isNetHovered ? 14 : 12,
                normal = { textColor = isNetHovered ? Color.white : _labelStyle.normal.textColor },
                wordWrap = false
            };
            Rect netLabelRect = new Rect(netRowRect.x, netRowRect.y, 120, netRowRect.height);
            GUI.Label(netLabelRect, labelContent, labelStyleLarger);
            
            // Period indicator in grey
            var periodStyle = new GUIStyle(_smallLabelStyle)
            {
                fontSize = isNetHovered ? 13 : 11,
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleRight
            };
            Rect periodRect = new Rect(netRowRect.x + netRowRect.width - 160, netRowRect.y, 60, netRowRect.height);
            GUI.Label(periodRect, $"({periodText})", periodStyle);
            
            double amount = snapshot.NetCashFlow;
            GUIStyle valueStyle;
            if (amount > 0)
                valueStyle = new GUIStyle(_valuePositiveStyle) { fontSize = isNetHovered ? 14 : 12 };
            else if (amount < 0)
                valueStyle = new GUIStyle(_valueNegativeStyle) { fontSize = isNetHovered ? 14 : 12 };
            else
                valueStyle = new GUIStyle(_valueNeutralStyle) { fontSize = isNetHovered ? 14 : 12 };
            
            Rect netValueRect = new Rect(netRowRect.x + netRowRect.width - 100, netRowRect.y, 100, netRowRect.height);
            GUI.Label(netValueRect, BudgetCalculator.FormatCurrency(amount, "N0"), valueStyle);

            EndCard();
        }


        /// <summary>
        /// Render a section header with optional action button
        /// </summary>
        public static void RenderSectionHeader(string title, string buttonText = null, Action buttonAction = null)
        {
            if (_headerStyle == null) InitializeStyles();

            GUILayout.BeginHorizontal(_headerStyle);
            GUILayout.Label(title, GUILayout.ExpandWidth(true));
            
            if (!string.IsNullOrEmpty(buttonText) && buttonAction != null)
            {
                if (GUILayout.Button(buttonText, _buttonStyle, GUILayout.Width(100)))
                {
                    buttonAction();
                }
            }
            
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Get a secondary text style for empty states and hints
        /// </summary>
        public static GUIStyle GetSecondaryTextStyle()
        {
            if (_smallLabelStyle == null) InitializeStyles();
            return _smallLabelStyle;
        }
    }
}
