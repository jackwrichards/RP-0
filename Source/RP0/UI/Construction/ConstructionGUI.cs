using System;
using System.Collections.Generic;
using UnityEngine;
using RP0.UI;

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
        // Current columns: Name, Limit, Status, Expand
        private const float ExpandArrowWidth = 22f;
        private const float LCNameWidth = 155f;
        private const float LimitWidth = 85f;
        private const float StatusWidth = 70f;

        // Column widths for vessel table
        private const float VesselIndent = 20f;
        private const float VesselNameWidth = 155f;
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
                // Render table header
                RenderLaunchComplexTableHeader();

                GUILayout.Space(2);

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

        private void RenderLaunchComplexTableHeader()
        {
            const float rowHeight = 28f;
            Rect headerRect = GUILayoutUtility.GetRect(LeftPanelWidth - 40f, rowHeight);

            // Draw header background
            if (Event.current.type == EventType.Repaint)
            {
                var headerBgTex = SharedUIComponents.MakeTex(2, 2, SharedUIComponents.Colors.HeaderBackground);
                GUI.DrawTexture(headerRect, headerBgTex);
            }

            float currentX = headerRect.x;

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SharedUIComponents.Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Column headers with tooltips
            GUI.Label(new Rect(currentX, headerRect.y, LCNameWidth, headerRect.height),
                new GUIContent("Name", "Launch Complex name"), headerStyle);
            currentX += LCNameWidth;

            // Right-side column headers removed per request
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
            string lcNameText = $"{launchComplex.Name} <color=#9aa0a6>(Limit: {launchComplex.SupportedMassAsPrettyText})</color>";
            GUI.Label(new Rect(currentX, rowRect.y, LCNameWidth, rowRect.height),
                lcNameText, nameStyle);
            currentX += LCNameWidth;

            // Limit moved next to LC name
            currentX += LimitWidth;

            // Status (hangars do not show status)
            bool isHangar = launchComplex.LCType == LaunchComplexType.Hangar;
            string status = isHangar ? string.Empty : (launchComplex.IsOperational ? "Idle" : "Building");
            var statusColor = launchComplex.IsOperational ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.7f, 0.3f);
            var statusStyle = new GUIStyle(cellStyle) { normal = { textColor = statusColor } };
            GUI.Label(new Rect(currentX, rowRect.y, StatusWidth, rowRect.height),
                status, statusStyle);
            currentX += StatusWidth;

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

            const float rowHeight = 28f;
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

            // Vessel Name (aligned with LC Name column)
            string vesselName = vessel.shipName;
            string vesselNameText = $"{vesselName} <color=#9aa0a6>({vessel.mass:N1}t)</color>";
            var vesselNameStyle = new GUIStyle(cellStyle) { richText = true };
            GUI.Label(new Rect(currentX + 20f, rowRect.y, LCNameWidth - 20f, rowRect.height),
                new GUIContent(vesselNameText, vessel.shipName), vesselNameStyle);
            currentX += LCNameWidth;

            // Mass moved next to vessel name
            currentX += LimitWidth;

            // Status (aligned with Status column)
            float progressFraction = GetProgressFraction(vessel, isProduction);
            string statusText = isProduction ? $"{progressFraction * 100f:0}%" : "Ready";
            Color statusColor = isProduction ? new Color(0.3f, 0.7f, 0.4f) : new Color(0.4f, 0.6f, 0.9f);
            var statusStyle = new GUIStyle(cellStyle) { normal = { textColor = statusColor } };
            GUI.Label(new Rect(currentX, rowRect.y, StatusWidth, rowRect.height),
                statusText, statusStyle);
            currentX += StatusWidth;

            // Actions (after arrow space)
            currentX += ExpandArrowWidth;
            float actionX = currentX;
        
            // For hangars, always show Launch (instant); for other LCs show Rollout if in production
            bool isHangar = _selectedLaunchComplex != null && _selectedLaunchComplex.LCType == LaunchComplexType.Hangar;
            bool showLaunchOnly = isHangar || !isProduction;
        
            string actionLabel = showLaunchOnly ? "Launch" : "Rollout";
            if (GUI.Button(new Rect(actionX, rowRect.y + 2, 52, rowRect.height - 4),
                new GUIContent(actionLabel, showLaunchOnly ? "Launch vessel" : "Begin rollout process"), HighLogic.Skin.button))
            {
                if (showLaunchOnly)
                    OnLaunchVessel(_selectedLaunchComplex, vessel);
                else
                    OnBeginRollout(_selectedLaunchComplex, vessel);
            }
            actionX += 54;

            if (GUI.Button(new Rect(actionX, rowRect.y + 2, 20, rowRect.height - 4),
                new GUIContent("✎", "Edit vessel configuration"), HighLogic.Skin.button))
            {
                OnEditVessel(vessel);
            }
            actionX += 22;

            if (GUI.Button(new Rect(actionX, rowRect.y + 2, 20, rowRect.height - 4),
                new GUIContent("✕", "Delete vessel"), HighLogic.Skin.button))
            {
                OnDeleteVessel(_selectedLaunchComplex, vessel, isProduction);
            }

            GUILayout.Space(1);
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

            // TODO: Begin rollout process
            Debug.Log($"[RP-1 Construction] Starting rollout of {vessel.shipName} from {launchComplex.Name}");
        }

        private void OnLaunchVessel(LaunchComplex launchComplex, VesselProject vessel)
        {
            if (launchComplex == null || vessel == null)
                return;

            // TODO: Launch vessel
            Debug.Log($"[RP-1 Construction] Launching {vessel.shipName} from {launchComplex.Name}");
        }

        private void OnEditVessel(VesselProject vessel)
        {
            if (vessel == null)
                return;

            // TODO: Open vessel in editor or show configuration window
            Debug.Log($"[RP-1 Construction] Editing vessel {vessel.shipName}");
        }

        private void OnDeleteVessel(LaunchComplex launchComplex, VesselProject vessel, bool isProduction)
        {
            if (launchComplex == null || vessel == null)
                return;

            // TODO: Show confirmation dialog and delete vessel
            Debug.Log($"[RP-1 Construction] Deleting vessel {vessel.shipName} from {(isProduction ? "production" : "storage")}");
        }
    }
}
