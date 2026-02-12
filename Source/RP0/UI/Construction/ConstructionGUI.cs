using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RP0.UI;
using KSP.UI.Screens;

namespace RP0.UI.Construction
{
    /// <summary>
    /// Construction tab UI with hierarchical view of Launch Complexes, Production, Storage, and Vessels
    /// </summary>
    public class ConstructionGUI : UIBase
    {
        private const float LeftPanelWidth = 520f;
        private const float IndentSection = 18f;
        private const float IndentVessel = 36f;
        private const float IndentVesselDetails = 58f;
        private const float ProgressPillWidth = 100f;
        private const float ProgressPillHeight = 22f;

        // Panel heights and widths
        private const float MainPanelHeight = 400f;
        private const float RightPanelMinWidth = 500f;

        private Vector2 _leftPanelScroll = Vector2.zero;
        private Vector2 _rightPanelScroll = Vector2.zero;
        private LaunchComplex _selectedLaunchComplex;

        private readonly Dictionary<Guid, bool> _expandedLaunchComplexes = new Dictionary<Guid, bool>();

        // Column widths for launch complex table
        // Current columns: Name (expanded), Status (right-aligned)
        private const float ExpandArrowWidth = 22f;
        private const float StatusWidth = 70f;

        // Column widths for vessel table
        private const float VesselIndent = 20f;
        private const float VesselNameWidth = 140f;
        private const float VesselMassWidth = 85f;
        private const float VesselSizeWidth = 85f;
        private const float VesselStatusWidth = 100f;
        private const float VesselActionsWidth = 110f;

        protected override void OnStart()
        {
            base.OnStart();
            SharedUIComponents.InitializeStyles();
        }

        /// <summary>
        /// Main construction tab rendering
        /// </summary>
        public void RenderConstructionTab()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(MainPanelHeight));

            RenderLeftPanel();

            GUILayout.Space(12);

            RenderRightPanel();

            GUILayout.EndHorizontal();
        }

        private void RenderLeftPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth), GUILayout.Height(MainPanelHeight));

            SharedUIComponents.BeginCard("Launch Complexes");

            _leftPanelScroll = GUILayout.BeginScrollView(_leftPanelScroll, GUILayout.Height(MainPanelHeight - 40f));

            var currentSC = SpaceCenterManagement.Instance?.ActiveSC;
            if (currentSC != null && currentSC.LaunchComplexes.Count > 0)
            {
                // Render launch complex rows
                foreach (LaunchComplex launchComplex in currentSC.LaunchComplexes)
                {
                    RenderLaunchComplexRow(launchComplex);
                }
            }
            else
            {
                GUILayout.Label("No launch complexes available", BoldLabel);
            }

            GUILayout.EndScrollView();

            SharedUIComponents.EndCard();

            GUILayout.EndVertical();
        }

        private void RenderLaunchComplexRow(LaunchComplex launchComplex)
        {
            if (launchComplex == null)
                return;

            Guid lcId = launchComplex.ID;
            bool isExpanded = IsExpanded(_expandedLaunchComplexes, lcId);
            bool isSelected = _selectedLaunchComplex != null && _selectedLaunchComplex.ID == lcId;

            const float rowHeight = 26f;
            Rect rowRect = GUILayoutUtility.GetRect(LeftPanelWidth - 40f, rowHeight);
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Handle row click to toggle expansion and select - only on the arrow area
            Rect arrowRect = new Rect(rowRect.x + rowRect.width - ExpandArrowWidth, rowRect.y, ExpandArrowWidth, rowRect.height);
            if (Event.current.type == EventType.MouseDown && arrowRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                ToggleExpanded(_expandedLaunchComplexes, lcId);
                SelectLaunchComplex(launchComplex);
                Event.current.Use();
            }

            // Handle row selection on click (anywhere but arrow)
            if (Event.current.type == EventType.MouseDown && isHovered && !arrowRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                SelectLaunchComplex(launchComplex);
                Event.current.Use();
            }

            // Draw background
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (isSelected)
                    bgColor = new Color(0.3f, 0.6f, 1.0f, 0.2f);
                else if (isHovered)
                    bgColor = new Color(1f, 1f, 1f, 0.08f);
                else
                    bgColor = new Color(0.12f, 0.12f, 0.14f, 0.4f);

                var bgTex = SharedUIComponents.MakeTex(2, 2, bgColor);
                GUI.DrawTexture(rowRect, bgTex);
            }

            float currentX = rowRect.x;

            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 15 : 14,
                normal = { textColor = SharedUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Name
            var nameStyle = new GUIStyle(cellStyle) { fontStyle = FontStyle.Bold, richText = true };
            string lcNameText = $"{launchComplex.Name} <color=#9aa0a6><size=11>(Limit: {launchComplex.SupportedMassAsPrettyText})</size></color>";
            GUI.Label(new Rect(currentX, rowRect.y, rowRect.width - StatusWidth - 10f, rowRect.height),
                lcNameText, nameStyle);

            // Status (hangars do not show status) - positioned on the right
            bool isHangar = launchComplex.LCType == LaunchComplexType.Hangar;
            string status = isHangar ? string.Empty : (launchComplex.IsOperational ? "Idle" : "Building");
            var statusColor = launchComplex.IsOperational ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.7f, 0.3f);
            var statusStyle = new GUIStyle(cellStyle) { normal = { textColor = statusColor } };
            GUI.Label(new Rect(rowRect.x + rowRect.width - StatusWidth - ExpandArrowWidth, rowRect.y, StatusWidth, rowRect.height),
                status, statusStyle);

            // Expand/collapse arrow indicator (right aligned)
            var arrowStyle = new GUIStyle(HighLogic.Skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUI.Label(new Rect(rowRect.x + rowRect.width - ExpandArrowWidth, rowRect.y, ExpandArrowWidth, rowRect.height),
                isExpanded ? "▼" : "▶", arrowStyle);

            GUILayout.Space(1);

            // Render vessels if expanded
            if (isExpanded)
            {
                RenderVesselsTable(launchComplex);
            }
        }

        private void RenderVesselsTable(LaunchComplex launchComplex)
        {
            bool hasProduction = launchComplex.BuildList != null && launchComplex.BuildList.Count > 0;
            bool hasStorage = launchComplex.Warehouse != null && launchComplex.Warehouse.Count > 0;

            if (!hasProduction && !hasStorage)
            {
                GUILayout.Space(2);
                Rect emptyRect = GUILayoutUtility.GetRect(LeftPanelWidth - 40f, 20f);
                var emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                    alignment = TextAnchor.MiddleLeft
                };
                GUI.Label(new Rect(emptyRect.x + 10f, emptyRect.y, emptyRect.width, emptyRect.height),
                    "No vessels in production or storage", emptyStyle);
                GUILayout.Space(2);
                return;
            }

            // Render vessel table header
            // Header disabled - showing all vessel info inline without headers
            // RenderVesselTableHeader();

            GUILayout.Space(2);

            // Render production vessels
            if (hasProduction)
            {
                foreach (VesselProject vessel in launchComplex.BuildList)
                {
                    RenderVesselRow(vessel, isProduction: true);
                }
            }

            // Render storage vessels
            if (hasStorage)
            {
                foreach (VesselProject vessel in launchComplex.Warehouse)
                {
                    RenderVesselRow(vessel, isProduction: false);
                }
            }

            GUILayout.Space(4);
        }

        private void RenderVesselTableHeader()
        {
            const float rowHeight = 24f;
            Rect headerRect = GUILayoutUtility.GetRect(LeftPanelWidth - 40f, rowHeight);

            // Draw subtle header background
            if (Event.current.type == EventType.Repaint)
            {
                var headerBgTex = SharedUIComponents.MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f, 0.5f));
                GUI.DrawTexture(headerRect, headerBgTex);
            }

            float currentX = headerRect.x + VesselIndent + ExpandArrowWidth;

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };

            GUI.Label(new Rect(currentX, headerRect.y, VesselNameWidth, headerRect.height),
                new GUIContent("Vessel Name", "Name of the vessel"), headerStyle);
            currentX += VesselNameWidth;

            GUI.Label(new Rect(currentX, headerRect.y, VesselMassWidth, headerRect.height),
                new GUIContent("Mass", "Vessel mass in tons"), headerStyle);
            currentX += VesselMassWidth;

            GUI.Label(new Rect(currentX, headerRect.y, VesselSizeWidth, headerRect.height),
                new GUIContent("Size", "Vessel dimensions"), headerStyle);
            currentX += VesselSizeWidth;

            GUI.Label(new Rect(currentX, headerRect.y, VesselStatusWidth, headerRect.height),
                new GUIContent("Status", "Build progress or status"), headerStyle);
            currentX += VesselStatusWidth;

            GUI.Label(new Rect(currentX, headerRect.y, VesselActionsWidth, headerRect.height),
                new GUIContent("Actions", "Available actions"), headerStyle);
        }

        private void RenderVesselRow(VesselProject vessel, bool isProduction)
        {
            if (vessel == null)
                return;

            const float rowHeight = 32f;
            Rect rowRect = GUILayoutUtility.GetRect(LeftPanelWidth - 40f, rowHeight);
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Draw background with subtle production/storage color coding
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (isHovered)
                    bgColor = new Color(1f, 1f, 1f, 0.05f);
                else if (isProduction)
                    bgColor = new Color(0.3f, 0.7f, 0.4f, 0.03f); // Subtle green tint
                else
                    bgColor = new Color(0.4f, 0.6f, 0.9f, 0.03f); // Subtle blue tint

                var bgTex = SharedUIComponents.MakeTex(2, 2, bgColor);
                GUI.DrawTexture(rowRect, bgTex);
            }

            float currentX = rowRect.x;

            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 14 : 13,
                normal = { textColor = SharedUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Add small indent indicator for hierarchy
            var indentStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            GUI.Label(new Rect(currentX, rowRect.y, 20f, rowRect.height), "  └", indentStyle);
            currentX += 20f;

            // Vessel Name with mass (aligned with LC Name column)
            string vesselName = vessel.shipName;
            string vesselNameText = $"{vesselName} <color=#9aa0a6>({vessel.mass:N1}t)</color>";
            var vesselNameStyle = new GUIStyle(cellStyle) { richText = true };
            float nameWidthAvailable = rowRect.width - StatusWidth - ExpandArrowWidth - 30f;
            Rect nameRect = new Rect(currentX, rowRect.y, nameWidthAvailable, rowRect.height);
            GUI.Label(nameRect, new GUIContent(vesselNameText, vessel.shipName), vesselNameStyle);
            
            // Handle click on name to edit
            if (Event.current.type == EventType.MouseDown && nameRect.Contains(Event.current.mousePosition))
            {
                OnEditVessel(vessel);
                Event.current.Use();
            }
            
            currentX += nameWidthAvailable;

            // Dynamic status display with progress bars
            RenderVesselStatusDisplay(vessel, isProduction, new Rect(currentX - 40f, rowRect.y, 220f, rowRect.height));

            GUILayout.Space(1);
        }

        private void RenderVesselStatusDisplay(VesselProject vessel, bool isProduction, Rect displayRect)
        {
            float barHeight = 14f;
            float barY = displayRect.y + (displayRect.height - barHeight) / 2f;
            float barWidth = 130f;
            float barX = displayRect.x;

            if (isProduction)
            {
                // Simple blue progress bar
                float progressFraction = GetProgressFraction(vessel, true);

                // Draw background
                if (Event.current.type == EventType.Repaint)
                {
                    var bgTex = SharedUIComponents.MakeTex(2, 2, new Color(0.2f, 0.2f, 0.22f, 0.8f));
                    GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), bgTex);

                    // Draw simple blue fill
                    Color blueColor = new Color(0.3f, 0.7f, 1f, 0.9f);
                    float fillWidth = barWidth * progressFraction;
                    Rect fillRect = new Rect(barX, barY, fillWidth, barHeight);
                    var fillTex = SharedUIComponents.MakeTex(2, 2, blueColor);
                    GUI.DrawTexture(fillRect, fillTex);
                }

                // Draw percentage text
                var percentStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(barX, barY - 3f, barWidth, barHeight + 6f), $"{progressFraction * 100f:F0}%", percentStyle);

                barX += barWidth + 6f;
            }
            else
            {
                // Simple orange bar for storage
                if (Event.current.type == EventType.Repaint)
                {
                    var bgTex = SharedUIComponents.MakeTex(2, 2, new Color(1f, 0.6f, 0.2f, 0.85f));
                    GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), bgTex);
                }

                // Draw "Storage" text
                var storageStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(barX, barY - 3f, barWidth, barHeight + 6f), "Storage", storageStyle);

                barX += barWidth + 6f;
            }

            // Action button (Rollout/Launch)
            bool isHangar = _selectedLaunchComplex != null && _selectedLaunchComplex.LCType == LaunchComplexType.Hangar;
            bool showLaunchOnly = isHangar || !isProduction;
            string actionLabel = showLaunchOnly ? "Launch" : "Rollout";
            
            float buttonWidth = 52f;
            Rect actionButtonRect = new Rect(barX, displayRect.y + 2f, buttonWidth, displayRect.height - 4f);
            
            if (GUI.Button(actionButtonRect, new GUIContent(actionLabel, showLaunchOnly ? "Launch vessel" : "Begin rollout process"), HighLogic.Skin.button))
            {
                if (showLaunchOnly)
                    OnLaunchVessel(_selectedLaunchComplex, vessel);
                else
                    OnBeginRollout(_selectedLaunchComplex, vessel);
            }

            barX += buttonWidth + 2f;

            // Delete button (red X)
            float deleteButtonWidth = 20f;
            Rect deleteButtonRect = new Rect(barX, displayRect.y + 2f, deleteButtonWidth, displayRect.height - 4f);
            var redButtonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
                hover = { textColor = new Color(1f, 0.5f, 0.5f) }
            };
            
            if (GUI.Button(deleteButtonRect, new GUIContent("✕", "Delete vessel"), redButtonStyle))
            {
                OnDeleteVessel(_selectedLaunchComplex, vessel, isProduction);
            }
        }

        private void RenderRightPanel()
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(RightPanelMinWidth), GUILayout.Height(MainPanelHeight), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            if (_selectedLaunchComplex != null)
            {
                SharedUIComponents.BeginCard(_selectedLaunchComplex.Name);

                _rightPanelScroll = GUILayout.BeginScrollView(_rightPanelScroll, GUILayout.Height(MainPanelHeight - 40f));

                GUILayout.Label("Launch Complex Settings", BoldLabel);
                GUILayout.Space(6);

                Color innerPanelColor = Color.Lerp(SharedUIComponents.Colors.CardBackground, Color.white, 0.08f);
                var innerPanelStyle = new GUIStyle(HighLogic.Skin.box)
                {
                    normal = { background = SharedUIComponents.MakeTex(2, 2, innerPanelColor) },
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(6, 6, 4, 4),
                    margin = new RectOffset(0, 0, 0, 0)
                };

                var panelTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = SharedUIComponents.Colors.TextPrimary }
                };

                var mutedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = SharedUIComponents.Colors.TextSecondary }
                };

                var bigCenterStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = SharedUIComponents.Colors.TextPrimary }
                };

                var centerMutedStyle = new GUIStyle(mutedStyle)
                {
                    alignment = TextAnchor.MiddleCenter
                };

                const float panelHeight = 120f;
                const float panelSpacing = 8f;

                double efficiency = _selectedLaunchComplex.Efficiency;
                double rateFull = _selectedLaunchComplex.Rate * efficiency * _selectedLaunchComplex.StrategyRateMultiplier;
                double buildRateMultiplier = _selectedLaunchComplex.Rate > 0 ? rateFull / _selectedLaunchComplex.Rate : 1d;
                string efficiencyText = $"{efficiency:P0}";
                string globalEfficiencyText = $"{LCEfficiency.MaxEfficiency:P0}";

                // Top row (3 panels)
                GUILayout.BeginHorizontal();

                // Panel 1: Dimensions/weight + human-rated
                GUILayout.BeginVertical(innerPanelStyle, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));
                GUILayout.Label("Limits", panelTitleStyle);
                GUILayout.Space(2);
                GUILayout.Label($"Max Height: {_selectedLaunchComplex.SizeMax.y:N1}m");
                GUILayout.Label($"Max Width: {_selectedLaunchComplex.SizeMax.x:N1}m");
                GUILayout.Label($"Human Rated: {(_selectedLaunchComplex.IsHumanRated ? "Yes" : "No")}");
                GUILayout.EndVertical();

                GUILayout.Space(panelSpacing);

                // Panel 2: Efficiency summary
                GUILayout.BeginVertical(innerPanelStyle, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                GUILayout.Label(efficiencyText, bigCenterStyle);
                GUILayout.Label("Efficiency", centerMutedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mult: {buildRateMultiplier:P0}", mutedStyle, GUILayout.ExpandWidth(true));
                GUILayout.Label($"Global: {globalEfficiencyText}", mutedStyle, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(panelSpacing);

                // Panel 3: Engineers slider
                GUILayout.BeginVertical(innerPanelStyle, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));
                GUILayout.Label("Engineers", panelTitleStyle);
                GUILayout.Space(2);
                int maxEngineers = _selectedLaunchComplex.MaxEngineers;
                int currentEngineers = _selectedLaunchComplex.Engineers;
                GUILayout.Label($"Assigned: {currentEngineers}/{maxEngineers}");
                int newEngineers = Mathf.RoundToInt(GUILayout.HorizontalSlider(currentEngineers, 0, maxEngineers));
                if (newEngineers != currentEngineers)
                {
                    _selectedLaunchComplex.Engineers = newEngineers;
                    MaintenanceHandler.Instance?.ScheduleMaintenanceUpdate();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.Space(panelSpacing);

                // Bottom row (2 panels)
                GUILayout.BeginHorizontal();

                // Panel 4: Operating costs
                GUILayout.BeginVertical(innerPanelStyle, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));
                GUILayout.Label("Operating Costs", panelTitleStyle);
                GUILayout.Space(2);
                double annualCost = (MaintenanceHandler.Instance?.LCUpkeep(_selectedLaunchComplex) ?? 0d) * 365.25;
                GUILayout.Label($"\u221a{annualCost:N0}/yr");
                GUILayout.EndVertical();

                GUILayout.Space(panelSpacing);

                // Panel 5: Launch pad list
                GUILayout.BeginVertical(innerPanelStyle, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));
                GUILayout.Label("Launch Pads", panelTitleStyle);
                GUILayout.Space(2);
                if (_selectedLaunchComplex.LaunchPads == null || _selectedLaunchComplex.LaunchPads.Count == 0)
                {
                    GUILayout.Label("None", mutedStyle);
                }
                else
                {
                    foreach (var pad in _selectedLaunchComplex.LaunchPads)
                    {
                        GUILayout.Label($"{pad.name} - {pad.State}");
                    }
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.Space(12);
                GUILayout.Label("Actions", BoldLabel);
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rename", HighLogic.Skin.button, GUILayout.Width(100)))
                {
                    // TODO: Open rename dialog
                }
                GUILayout.Space(4);
                if (GUILayout.Button("Dismantle", HighLogic.Skin.button, GUILayout.Width(100)))
                {
                    // TODO: Dismantle pad with confirmation
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label("Additional settings coming soon...", SharedUIComponents.GetSecondaryTextStyle());

                GUILayout.EndScrollView();

                SharedUIComponents.EndCard();
            }
            else
            {
                SharedUIComponents.BeginCard("Select a Launch Complex");
                GUILayout.Label("Select a launch complex from the left panel to view and edit its settings.", BoldLabel);
                SharedUIComponents.EndCard();
            }

            GUILayout.EndVertical();
        }

        private static float GetProgressFraction(VesselProject vessel, bool isProduction)
        {
            if (!isProduction)
                return 1f;

            if (vessel == null || vessel.buildPoints <= 0d)
                return 0f;

            return Mathf.Clamp01((float)(vessel.progress / vessel.buildPoints));
        }

        private static void RenderProgressBar(Rect rect, float fraction, string label, Color fillColor)
        {
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);

            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, SharedUIComponents.MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f)));
                GUI.DrawTexture(fillRect, SharedUIComponents.MakeTex(2, 2, fillColor));
            }

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            GUI.Label(rect, label, labelStyle);
        }

        private static bool IsExpanded(Dictionary<Guid, bool> map, Guid id)
        {
            return map.TryGetValue(id, out bool isExpanded) && isExpanded;
        }

        private static void ToggleExpanded(Dictionary<Guid, bool> map, Guid id)
        {
            map[id] = !IsExpanded(map, id);
        }

        private void SelectLaunchComplex(LaunchComplex lc)
        {
            _selectedLaunchComplex = lc;
        }

        private void OnBeginRollout(LaunchComplex launchComplex, VesselProject vessel)
        {
            if (launchComplex == null || vessel == null)
                return;

            string launchSite = vessel.launchSite;
            if (launchSite == "LaunchPad" && launchComplex.LCType == LaunchComplexType.Pad)
            {
                if (vessel.launchSiteIndex >= 0 && vessel.launchSiteIndex < launchComplex.LaunchPads.Count)
                    launchSite = launchComplex.LaunchPads[vessel.launchSiteIndex].name;
                else
                    launchSite = launchComplex.ActiveLPInstance.name;
            }

            // Check if there's a pad available
            LCLaunchPad foundPad = launchComplex.FindFreeLaunchPad();
            LaunchPadState padState = foundPad != null ? LaunchPadState.Free : launchComplex.GetBestLaunchPadState();

            if (padState <= LaunchPadState.Nonoperational)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                    "cannotRollOutDestroyedPopup", "Cannot Roll out!", 
                    "The launch pad is not operational. You must repair it before you can roll out.", 
                    "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                return;
            }

            // Check facility requirements
            List<string> facilityChecks = new List<string>();
            bool meetsChecks = vessel.MeetsFacilityRequirements(facilityChecks);

            if (!meetsChecks)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                    "cannotLaunchEditorChecksPopup", "Cannot Launch!", 
                    "Warning! This vessel did not pass the editor checks! Until you upgrade this launch complex it cannot be launched. Listed below are the failed checks:\n" 
                    + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), 
                    "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                return;
            }

            // Create rollout project
            ReconRolloutProject rollout = new ReconRolloutProject(vessel, ReconRolloutProject.RolloutReconType.Rollout, vessel.shipID.ToString(), launchSite);
            
            if (foundPad != null)
            {
                bool padClear = !foundPad.HasVesselWaitingToBeLaunched(out Vessel foundVessel);
                if (padClear)
                {
                    vessel.launchSiteIndex = launchComplex.LaunchPads.IndexOf(foundPad);
                    launchComplex.Recon_Rollout.Add(rollout);
                    RP0Debug.Log($"[RP-1 Construction] Starting rollout of {vessel.shipName} to {launchSite}");
                }
                else
                {
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                        "cannotRollOutVesselOnPad", "Cannot Roll out!", 
                        $"{foundVessel.vesselName} is already waiting on the launch pad.", 
                        "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                }
            }
            else
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    "cannotRollOutNoPad", "Cannot Roll out!",
                    "No launch pad is currently free. Please free a pad or wait for the active rollout to complete.",
                    "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
            }
        }

        private void OnLaunchVessel(LaunchComplex launchComplex, VesselProject vessel)
        {
            if (launchComplex == null || vessel == null)
                return;

            string launchSite = vessel.launchSite;
            bool isPad = launchComplex.LCType == LaunchComplexType.Pad;

            if (launchSite == "LaunchPad" && isPad)
            {
                if (vessel.launchSiteIndex >= 0 && vessel.launchSiteIndex < launchComplex.LaunchPads.Count)
                    launchSite = launchComplex.LaunchPads[vessel.launchSiteIndex].name;
                else
                    launchSite = launchComplex.ActiveLPInstance.name;
            }

            // Get the launch pad
            LCLaunchPad pad = isPad ? launchComplex.LaunchPads.Find(lp => lp.name == launchSite) : null;

            // Verify pad is operational
            if (isPad && (pad == null || pad.IsDestroyed || !pad.isOperational))
            {
                string msg = pad == null ? "No launch pad found." : "The launch pad requires repairs.";
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                    "cannotLaunchRepairPopup", "Cannot Launch!", 
                    msg, "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                return;
            }

            // Check facility requirements
            List<string> facilityChecks = new List<string>();
            if (!vessel.MeetsFacilityRequirements(facilityChecks))
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                    "cannotLaunchEditorChecksPopup", "Cannot Launch!", 
                    "Warning! This vessel did not pass the editor checks! Until you upgrade this launch complex it cannot be launched. Listed below are the failed checks:\n" 
                    + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), 
                    "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                return;
            }

            // Check if there's another vessel already on the pad
            if (isPad && ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, pad.launchSiteName).Count > 0)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                    "cannotLaunchPadBusy", "Cannot Launch!", 
                    "There is already a vessel on this launch pad.", "Acknowledged", false, HighLogic.UISkin).HideGUIsWhilePopup();
                return;
            }

            // Set up for launch
            SpaceCenterManagement.Instance.LaunchedVessel = vessel;
            
            if (isPad)
            {
                launchComplex.SwitchLaunchPad(vessel.launchSiteIndex);
            }

            // Handle crew assignment if needed
            if (vessel.IsCrewable())
            {
                RP0Debug.Log($"[RP-1 Construction] Launching {vessel.shipName} - needs crew assignment");
                // TODO: Show crew assignment window if needed
            }

            // Actually launch the vessel
            vessel.Launch();
            RP0Debug.Log($"[RP-1 Construction] Launched {vessel.shipName} from {launchSite}");
        }

        private void OnEditVessel(VesselProject vessel)
        {
            if (vessel == null)
                return;

            // This would typically open the vessel in the editor
            // For now, log the action
            RP0Debug.Log($"[RP-1 Construction] Opening vessel editor for {vessel.shipName}");
            
            // TODO: Integrate with editor if needed
            // string tempFile = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}/Ships/temp.craft";
            // vessel.UpdateNodeAndSave(tempFile);
            // SpaceCenterManagement.Instance.EditedVessel = vessel;
            // EditorDriver.StartAndLoadVessel(tempFile, vessel.Type == ProjectType.SPH ? EditorFacility.SPH : EditorFacility.VAB);
        }

        private void OnDeleteVessel(LaunchComplex launchComplex, VesselProject vessel, bool isProduction)
        {
            if (launchComplex == null || vessel == null)
                return;

            DialogGUIBase[] options = new DialogGUIBase[2];
            options[0] = new DialogGUIButton("Yes", () => ConfirmDeleteVessel(launchComplex, vessel, isProduction));
            options[1] = new DialogGUIButton("No", () => { });
            
            string location = isProduction ? "production" : "storage";
            MultiOptionDialog dialog = new MultiOptionDialog(
                "deleteVesselConfirmation", 
                $"Are you sure you want to delete {vessel.shipName} from {location}?", 
                "Delete Vessel?", 
                null, 
                options);
            
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin).HideGUIsWhilePopup();
        }

        private void ConfirmDeleteVessel(LaunchComplex launchComplex, VesselProject vessel, bool isProduction)
        {
            if (isProduction)
            {
                launchComplex.BuildList.Remove(vessel);
            }
            else
            {
                launchComplex.Warehouse.Remove(vessel);
            }
            
            RP0Debug.Log($"[RP-1 Construction] Deleted vessel {vessel.shipName}");
        }
    }
}
