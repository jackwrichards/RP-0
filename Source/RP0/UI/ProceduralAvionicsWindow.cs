using ClickThroughFix;
using RealFuels.Tanks;
using ROUtils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UniLinq;
using static RP0.ProceduralAvionics.ProceduralAvionicsUtils;

namespace RP0.ProceduralAvionics
{
    public class ProceduralAvionicsWindow : MonoBehaviour
    {
        private static readonly int _windowId = "RP0ProcAviWindow".GetHashCode();

        private Rect _windowRect = new Rect(267, 104, 650, 500);
        private GUIContent _gc;
        private string[] _avionicsConfigNames;
        private int _selectedConfigIndex = 0;
        private float _newControlMass;
        private string _sECAmount = "400";
        private string _sExtraVolume = "0";
        private bool _showROTankSizeWarning;
        private bool _showSizeWarning;
        private bool _shouldResetUIHeight;
        private Vector2 _techLevelScrollPos = Vector2.zero;
        private Dictionary<string, string> _tooltipTexts;
        private ModuleProceduralAvionics _module;
        private ModuleFuelTanks _rfPM;
        private FuelTank _ecTank;
        private bool _showMassBreakdown = true;

        public string ControllableMass { get; set; }

        public void ShowForModule(ModuleProceduralAvionics module)
        {
            _module = module;
            ControllableMass = $"{module.controllableMass:0.###}";

            FetchRFModule(_module, out ModuleFuelTanks rfPM, out FuelTank ecTank);
            _rfPM = rfPM;
            _ecTank = ecTank;

            RefreshDisplays();
        }

        public void OnTankDefinitionChanged()
        {
            // RF will regenerate all the FuelTank instances if Tank Definition is changed.
            // Thus need to refetch EC tank.
            FetchRFModule(_module, out ModuleFuelTanks rfPM, out FuelTank ecTank);
            _rfPM = rfPM;
            _ecTank = ecTank;
        }

        public void RefreshDisplays()
        {
            _sECAmount = $"{_ecTank.maxAmount:F0}";
            _sExtraVolume = $"{_rfPM.AvailableVolume:0.#}";
        }

        public void OnGUI()
        {
            if (_module != null && _module.showGUI)
            {
                if (_avionicsConfigNames == null)
                {
                    _avionicsConfigNames = ProceduralAvionicsTechManager.GetAllConfigs().ToArray();
                    _selectedConfigIndex = _avionicsConfigNames.IndexOf(_module.avionicsConfigName);
                    _tooltipTexts = new Dictionary<string, string>();
                }

                if (_shouldResetUIHeight && Event.current.type == EventType.Layout)
                {
                    _windowRect.height = 500;
                    _shouldResetUIHeight = false;
                }
                _windowRect = ClickThruBlocker.GUILayoutWindow(_windowId, _windowRect, WindowFunction, "", HighLogic.Skin.window);
                Tooltip.Instance.ShowTooltip(_windowId, contentAlignment: TextAnchor.MiddleLeft);
            }
        }

        private void WindowFunction(int windowID)
        {
            RP0.UI.SharedUIComponents.InitializeStyles();

            // Horizontal bar visualization section (above table)
            RenderAvionicsTypeVisualization();

            GUILayout.Space(10);

            // Tech Level Selection Table
            RenderTechLevelTable();

            GUILayout.Space(10);

            // Control inputs section
            RenderControlInputs();

            GUI.DragWindow();
        }

        private void RenderTypeSelection()
        {
            RP0.UI.SharedUIComponents.BeginCard("Avionics Type");

            // Display the combined name with tech level (e.g., "Near Earth" + "Post-War Avionics")
            string configName = _avionicsConfigNames[_selectedConfigIndex];
            ProceduralAvionicsConfig config = ProceduralAvionicsTechManager.GetProceduralAvionicsConfig(configName);
            
            // Get current tech level name
            string techLevelName = _module.CurrentProceduralAvionicsTechNode?.dispName ?? _module.CurrentProceduralAvionicsTechNode?.name ?? "Unknown";
            
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = config.IsAvailable ? RP0.UI.SharedUIComponents.Colors.TextPrimary : new Color(1f, 0.65f, 0.3f) },
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };

            var techLevelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextSecondary },
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };

            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            _gc ??= new GUIContent();
            _gc.text = configName;
            _gc.tooltip = config.description;
            
            GUILayout.Label(_gc, labelStyle, GUILayout.Width(150));
            
            // Display tech level below the config name
            GUILayout.Label(techLevelName, techLevelStyle, GUILayout.Width(150));
            
            GUILayout.Space(5);
            GUILayout.EndVertical();

            RP0.UI.SharedUIComponents.EndCard();
        }

        private void RenderConfigurationPanel()
        {
            RP0.UI.SharedUIComponents.BeginCard("Configuration");

            GUILayout.BeginHorizontal();

            // Left column: Input fields
            GUILayout.BeginVertical(GUILayout.Width(280));

            // Controllable Mass row
            GUILayout.BeginHorizontal();
            GUILayout.Label("Contr. Mass:", HighLogic.Skin.label, GUILayout.Width(90));
            ControllableMass = GUILayout.TextField(ControllableMass, HighLogic.Skin.textField, GUILayout.Width(60));
            GUILayout.Label("t", HighLogic.Skin.label, GUILayout.Width(15));
            GUILayout.Space(5);
            
            float oldControlMass = _newControlMass;
            if (float.TryParse(ControllableMass, out _newControlMass))
            {
                float avionicsMass = _module.GetShieldedAvionicsMass(_newControlMass);
                float ekW = ModuleProceduralAvionics.GetEnabledkW(_module.CurrentProceduralAvionicsTechNode, _newControlMass);
                GUILayout.Label($"{ekW:F0}W", HighLogic.Skin.label, GUILayout.Width(55));
            }
            else
            {
                GUILayout.Label("-", HighLogic.Skin.label, GUILayout.Width(55));
            }

            if (oldControlMass != _newControlMass)
            {
                _tooltipTexts.Clear();
            }
            GUILayout.EndHorizontal();

            // EC Amount row
            GUILayout.BeginHorizontal();
            GUILayout.Label("EC Amount:", HighLogic.Skin.label, GUILayout.Width(90));
            _sECAmount = GUILayout.TextField(_sECAmount, HighLogic.Skin.textField, GUILayout.Width(60));
            GUILayout.Label("kJ", HighLogic.Skin.label, GUILayout.Width(15));
            GUILayout.Space(5);
            
            if (float.TryParse(_sECAmount, out float ecAmount))
            {
                float ecMass = _ecTank.mass * ecAmount / _ecTank.utilization;
                GUILayout.Label($"{MathUtils.PrintMass(ecMass)}", HighLogic.Skin.label, GUILayout.Width(55));
            }
            else
            {
                GUILayout.Label("-", HighLogic.Skin.label, GUILayout.Width(55));
            }
            GUILayout.EndHorizontal();

            // Extra Volume row
            GUILayout.BeginHorizontal();
            _gc ??= new GUIContent();
            _gc.text = "Extra Volume:";
            GUILayout.Label(_gc, HighLogic.Skin.label, GUILayout.Width(90));
            GUI.enabled = false;
            _sExtraVolume = GUILayout.TextField(_sExtraVolume, HighLogic.Skin.textField, GUILayout.Width(60));
            GUI.enabled = true;
            GUILayout.Label("L", HighLogic.Skin.label, GUILayout.Width(15));
            GUILayout.Space(5);
            
            float avVolume = _module.GetAvionicsVolume();
            GUILayout.Label($"Min {avVolume:F1}m³", HighLogic.Skin.label, GUILayout.Width(55));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Warning messages
            if (_showROTankSizeWarning)
            {
                var warningStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = RP0.UI.SharedUIComponents.Colors.Warning },
                    fontSize = 10,
                    wordWrap = true
                };
                GUILayout.Label("ROTanks does not support automatic resizing. Increase part size manually.", warningStyle);
            }
            else if (_showSizeWarning)
            {
                var warningStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = RP0.UI.SharedUIComponents.Colors.Warning },
                    fontSize = 10,
                    wordWrap = true
                };
                GUILayout.Label("Not enough volume. Increase part size manually.", warningStyle);
            }

            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Right column: Apply buttons
            GUILayout.BeginVertical(GUILayout.Width(120));
            
            _gc.text = "Apply (Fit)";
            _gc.tooltip = "Applies the parameters and resizes the part to have the optimal amount of volume";
            if (GUILayout.Button(_gc, HighLogic.Skin.button, GUILayout.Height(35)))
            {
                ApplyAvionicsSettings(shouldSeekVolume: true);
            }

            _gc.text = "Apply (Preserve)";
            _gc.tooltip = "Applies the parameters but doesn't resize the part even if there isn't enough volume";
            if (GUILayout.Button(_gc, HighLogic.Skin.button, GUILayout.Height(35)))
            {
                ApplyAvionicsSettings(shouldSeekVolume: false);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            RP0.UI.SharedUIComponents.EndCard();
        }

        private void RenderTechLevelTable()
        {
            string curCfgName = _avionicsConfigNames[_selectedConfigIndex];
            ProceduralAvionicsConfig curCfg = ProceduralAvionicsTechManager.GetProceduralAvionicsConfig(curCfgName);

            RP0.UI.SharedUIComponents.BeginCard("");

            // Top center: Tabs for switching between avionics types
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            RenderAvionicsTypeTabs();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Column headers
            Rect headerRowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(24));
            
            float currentX = headerRowRect.x;
            
            // Define header rectangles for hover detection
            Rect nameHeaderRect = new Rect(currentX, headerRowRect.y, 150, headerRowRect.height);
            currentX += 150;
            Rect massHeaderRect = new Rect(currentX, headerRowRect.y, 90, headerRowRect.height);
            currentX += 90;
            Rect powerHeaderRect = new Rect(currentX, headerRowRect.y, 90, headerRowRect.height);
            currentX += 90;
            Rect idleHeaderRect = new Rect(currentX, headerRowRect.y, 50, headerRowRect.height);
            currentX += 50;
            Rect contHeaderRect = new Rect(currentX, headerRowRect.y, 40, headerRowRect.height);
            currentX += 40;
            Rect axialHeaderRect = new Rect(currentX, headerRowRect.y, 40, headerRowRect.height);
            currentX += 50;
            Rect actionHeaderRect = new Rect(currentX, headerRowRect.y, 90, headerRowRect.height);
            
            // Detect hover
            bool hoverName = nameHeaderRect.Contains(Event.current.mousePosition);
            bool hoverMass = massHeaderRect.Contains(Event.current.mousePosition);
            bool hoverPower = powerHeaderRect.Contains(Event.current.mousePosition);
            bool hoverIdle = idleHeaderRect.Contains(Event.current.mousePosition);
            bool hoverCont = contHeaderRect.Contains(Event.current.mousePosition);
            bool hoverAxial = axialHeaderRect.Contains(Event.current.mousePosition);
            bool hoverAction = actionHeaderRect.Contains(Event.current.mousePosition);
            
            _gc ??= new GUIContent();
            
            // Name header
            var nameHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverName ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Name";
            _gc.tooltip = "Tech level display name";
            GUI.Label(nameHeaderRect, _gc, nameHeaderStyle);
            
            // Mass header
            var massHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverMass ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Mass";
            _gc.tooltip = "Avionics hardware mass in kilograms. Shows delta vs current config.";
            GUI.Label(massHeaderRect, _gc, massHeaderStyle);
            
            // Power header
            var powerHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverPower ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Power";
            _gc.tooltip = "Active power consumption in watts. Shows delta vs current config.";
            GUI.Label(powerHeaderRect, _gc, powerHeaderStyle);
            
            // Idle header
            var idleHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverIdle ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Idle";
            _gc.tooltip = "Hibernation power consumption in watts when the avionics is disabled.";
            GUI.Label(idleHeaderRect, _gc, idleHeaderStyle);
            
            // Container header
            var contHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverCont ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Cont.";
            _gc.tooltip = "Whether samples can be transferred and stored in the avionics unit.";
            GUI.Label(contHeaderRect, _gc, contHeaderStyle);
            
            // Axial header
            var axialHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverAxial ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Axial";
            _gc.tooltip = "Whether fore-aft translation is allowed despite insufficient controllable mass.";
            GUI.Label(axialHeaderRect, _gc, axialHeaderStyle);
            
            // Action header
            var actionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = hoverAction ? 13 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            _gc.text = "Action";
            _gc.tooltip = "Available actions: Switch to this config or Unlock/Purchase it.";
            GUI.Label(actionHeaderRect, _gc, actionHeaderStyle);

            // Tech level rows (scrollable) - fixed height
            int rowHeight = 22;
            int fixedVisibleRows = 8; // Fixed number of visible rows
            int scrollViewHeight = fixedVisibleRows * rowHeight;
            
            _techLevelScrollPos = GUILayout.BeginScrollView(_techLevelScrollPos, GUILayout.Height(scrollViewHeight));

            foreach (ProceduralAvionicsTechNode techNode in curCfg.TechNodes.Values)
            {
                RenderTechLevelRow(curCfg, techNode);
            }

            GUILayout.EndScrollView();

            RP0.UI.SharedUIComponents.EndCard();
        }

        private void RenderAvionicsTypeTabs()
        {
            GUILayout.BeginHorizontal();

            // Left-aligned title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextPrimary }
            };
            GUILayout.Label("Avionics Type Browser", titleStyle);

            // Push buttons to the right
            GUILayout.FlexibleSpace();

            int oldConfigIdx = _selectedConfigIndex;
            for (int i = 0; i < _avionicsConfigNames.Length; i++)
            {
                string configName = _avionicsConfigNames[i];
                ProceduralAvionicsConfig config = ProceduralAvionicsTechManager.GetProceduralAvionicsConfig(configName);
                
                bool isCurrent = i == _selectedConfigIndex;

                _gc ??= new GUIContent();
                _gc.text = configName;
                _gc.tooltip = config.description;

                // Create tab button style based on state
                GUIStyle tabStyle;
                if (isCurrent)
                {
                    // Active tab style - looks pressed/selected
                    tabStyle = new GUIStyle(HighLogic.Skin.button)
                    {
                        normal = HighLogic.Skin.button.active,
                        fontStyle = FontStyle.Bold,
                        fontSize = 15
                    };
                }
                else
                {
                    // Inactive tab style
                    tabStyle = new GUIStyle(HighLogic.Skin.button)
                    {
                        fontSize = 15
                    };
                }

                if (GUILayout.Button(_gc, tabStyle, GUILayout.Width(100), GUILayout.Height(26)))
                {
                    _selectedConfigIndex = i;
                }

                if (i < _avionicsConfigNames.Length - 1)
                {
                    GUILayout.Space(2);
                }
            }

            if (oldConfigIdx != _selectedConfigIndex)
            {
                _shouldResetUIHeight = true;
                _tooltipTexts.Clear();
            }

            GUILayout.EndHorizontal();
        }

        private void RenderTechLevelRow(ProceduralAvionicsConfig curCfg, ProceduralAvionicsTechNode techNode)
        {
            bool isCurrent = techNode == _module.CurrentProceduralAvionicsTechNode;
            int unlockCost = ProceduralAvionicsTechManager.GetUnlockCost(curCfg.name, techNode);

            // Calculate row rect for hover detection
            Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(22));
            bool isHovered = rowRect.Contains(Event.current.mousePosition);

            // Background color
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (isCurrent)
                    bgColor = new Color(0.3f, 0.6f, 1.0f, 0.15f); // Blue tint for current
                else if (!techNode.IsAvailable)
                    bgColor = new Color(1f, 0.5f, 0.3f, 0.08f); // Orange tint for locked
                else if (isHovered)
                    bgColor = new Color(1f, 1f, 1f, 0.05f); // Subtle hover
                else
                    bgColor = Color.clear;

                if (bgColor.a > 0)
                {
                    var bgTex = RP0.UI.SharedUIComponents.MakeTex(2, 2, bgColor);
                    GUI.DrawTexture(rowRect, bgTex);
                }
            }

            // Cell style
            var cellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = isHovered ? 16 : 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = isHovered ? RP0.UI.SharedUIComponents.Colors.TextPrimary : RP0.UI.SharedUIComponents.Colors.TextSecondary }
            };

            float currentX = rowRect.x;

            // Name
            string displayName = techNode.dispName ?? techNode.name;
            displayName = $"<b>{displayName}</b>";
            
            if (!techNode.IsAvailable)
            {
                var lockedStyle = new GUIStyle(cellStyle)
                {
                    normal = { textColor = new Color(1f, 0.65f, 0.3f) } // Orange for locked
                };
                GUI.Label(new Rect(currentX, rowRect.y, 150, rowRect.height), displayName, lockedStyle);
            }
            else
            {
                GUI.Label(new Rect(currentX, rowRect.y, 150, rowRect.height), displayName, cellStyle);
            }
            currentX += 150;

            // Calculate deltas for inline display
            float currentMassKG = 0;
            float currentPowerWatts = 0;
            if (_module.CurrentProceduralAvionicsTechNode != null)
            {
                ModuleProceduralAvionics.GetStatsForTechNode(_module.CurrentProceduralAvionicsTechNode, _newControlMass, out currentMassKG, out _, out currentPowerWatts);
            }

            // Mass with delta
            float calcMass = ModuleProceduralAvionics.GetStatsForTechNode(techNode, _newControlMass, out float massKG, out _, out float powerWatts);
            float deltaMass = massKG - currentMassKG;
            
            string massText = isCurrent ? $"{massKG:F0}kg" : $"{massKG:F0}kg <color=#999999><size=10>({deltaMass:+0;-0})</size></color>";
            GUI.Label(new Rect(currentX, rowRect.y, 90, rowRect.height), massText, cellStyle);
            currentX += 90;

            // Power (active) with delta
            float deltaPower = powerWatts - currentPowerWatts;
            string powerText = isCurrent ? $"{powerWatts:F0}W" : $"{powerWatts:F0}W <color=#999999><size=10>({deltaPower:+0;-0})</size></color>";
            GUI.Label(new Rect(currentX, rowRect.y, 90, rowRect.height), powerText, cellStyle);
            currentX += 90;

            // Idle Power (hibernation)
            float idlePower = powerWatts * techNode.disabledPowerFactor;
            string idlePowerText = techNode.disabledPowerFactor > 0 ? $"{idlePower:F0}W" : "-";
            GUI.Label(new Rect(currentX, rowRect.y, 50, rowRect.height), idlePowerText, cellStyle);
            currentX += 50;

            // Container
            string containerSymbol = techNode.hasScienceContainer ? "<color=#4CAF50>✓</color>" : "<color=#F44336>✗</color>";
            GUI.Label(new Rect(currentX, rowRect.y, 40, rowRect.height), containerSymbol, cellStyle);
            currentX += 40;

            // Axial
            string axialSymbol = techNode.allowAxial ? "<color=#4CAF50>✓</color>" : "<color=#F44336>✗</color>";
            GUI.Label(new Rect(currentX, rowRect.y, 40, rowRect.height), axialSymbol, cellStyle);
            currentX += 40;

            // Action column
            if (!isCurrent)
            {
                // Switch button
                Rect buttonRect = new Rect(currentX, rowRect.y + 1, 60, rowRect.height - 2);
                GUI.enabled = techNode.IsAvailable;
                if (GUI.Button(buttonRect, "Switch", HighLogic.Skin.button))
                {
                    SwitchToTechNode(curCfg, techNode);
                }
                GUI.enabled = true;
            }
            currentX += 65;
            
            // Unlock button if needed (show even when current)
            if (unlockCost > 0)
            {
                var cmq = CurrencyModifierQueryRP0.RunQuery(TransactionReasonsRP0.PartOrUpgradeUnlock, -unlockCost, 0d, 0d);
                double trueCost = -cmq.GetTotal(CurrencyRP0.Funds, false);
                double creditToUse = Math.Min(trueCost, UnlockCreditHandler.Instance.TotalCredit);
                cmq.AddPostDelta(CurrencyRP0.Funds, creditToUse, true);
                
                GUI.enabled = techNode.IsAvailable && cmq.CanAfford();
                Rect unlockRect = new Rect(currentX, rowRect.y + 1, 60, rowRect.height - 2);
                string costStr = BuildCostString(Math.Max(0d, trueCost - creditToUse), trueCost);
                if (GUI.Button(unlockRect, $"√{costStr}", HighLogic.Skin.button))
                {
                    if (ModuleProceduralAvionics.PurchaseConfig(curCfg.name, techNode))
                    {
                        SwitchToTechNode(curCfg, techNode);
                    }
                }
                GUI.enabled = true;
            }
        }

        private void SwitchToTechNode(ProceduralAvionicsConfig curCfg, ProceduralAvionicsTechNode techNode)
        {
            Log("Configuration window changed, updating part window");
            _shouldResetUIHeight = true;
            _showROTankSizeWarning = false;
            _showSizeWarning = false;

            _module.UpdateInSymmetry((ModuleProceduralAvionics pm) =>
            {
                pm.avionicsTechLevel = techNode.name;
                pm.CurrentProceduralAvionicsConfig = curCfg;
                pm.avionicsConfigName = curCfg.name;
                pm.AvionicsConfigChanged();
            });
            MonoUtilities.RefreshContextWindows(_module.part);
        }

        private void ApplyAvionicsSettings(bool shouldSeekVolume)
        {
            if (!float.TryParse(ControllableMass, out float newControlMass) || newControlMass < 0)
            {
                ScreenMessages.PostScreenMessage("Invalid controllable mass value");
                ControllableMass = $"{_module.controllableMass:0.###}";
                return;
            }

            if (!float.TryParse(_sECAmount, out float ecAmount) || ecAmount <= 0)
            {
                ScreenMessages.PostScreenMessage("EC amount needs to be larger than 0");
                _sECAmount = $"{_ecTank.maxAmount:F0}";
                return;
            }

            _module.UpdateInSymmetry((ModuleProceduralAvionics pm) =>
            {
                FetchRFModule(pm, out ModuleFuelTanks rfPM, out FuelTank ecTank);
                pm.controllableMass = newControlMass;

                if (shouldSeekVolume && pm.CanSeekVolume)
                {
                    pm.SetProcPartVolumeLimit();
                    ApplyCorrectProcTankVolume(pm, rfPM, ecTank, 0, ecAmount);
                }
                else
                {
                    bool isOverVolumeLimit = pm.ClampControllableMass();
                    if (isOverVolumeLimit)
                        pm.SetProcPartVolumeLimit();

                    // ROTank probe cores do not support SeekVolume()
                    _showROTankSizeWarning = isOverVolumeLimit && shouldSeekVolume && !pm.CanSeekVolume;
                    _showSizeWarning = isOverVolumeLimit && !shouldSeekVolume;
                    pm.UpdateControllableMassSlider();
                    pm.SendRemainingVolume();

                    _shouldResetUIHeight = true;

                    // In this case need to clamp EC amount to ensure that it stays within the available volume
                    float avVolume = pm.GetAvionicsVolume();
                    float m3AvailVol = pm.GetAvailableVolume();
                    double m3CurVol = avVolume + rfPM.totalVolume / 1000;    // l to m³, assume 100% RF utilization
                    double m3MinVol = GetNeededProcTankVolume(pm, rfPM, ecTank, 0, ecAmount);
                    double m3MissingVol = m3MinVol - m3CurVol;
                    if (m3MissingVol > 1e-6)
                    {
                        ecAmount = 1;    // Never remove the EC resource entirely
                        double m3AvailVolForEC = m3AvailVol;
                        if (m3AvailVolForEC > 0)
                        {
                            ecAmount = GetECAmountForVolume(rfPM, ecTank, (float)m3AvailVolForEC);
                        }
                    }
                }

                Log($"Applying RF tank amount values to {ecAmount}, currently has {ecTank.amount}/{ecTank.maxAmount}, volume {rfPM.AvailableVolume}");
                ecTank.maxAmount = ecAmount;
                ecTank.amount = ecAmount;
                rfPM.PartResourcesChanged();
                rfPM.CalculateMass();
                pm.RefreshDisplays();
            });

            MonoUtilities.RefreshContextWindows(_module.part);
        }

        private void ApplyCorrectProcTankVolume(ModuleProceduralAvionics pm, ModuleFuelTanks rfPM, FuelTank ecTank, float extraVolumeLiters, float ecAmount)
        {
            float m3TotalVolume = GetNeededProcTankVolume(pm, rfPM, ecTank, extraVolumeLiters, ecAmount);
            float avVolume = pm.GetAvionicsVolume();
            Log($"Applying volume {m3TotalVolume}; avionics: {avVolume * 1000}l; tanks: {(m3TotalVolume - avVolume) * 1000}l");
            pm.SeekVolume(m3TotalVolume);
        }

        private float GetNeededProcTankVolume(ModuleProceduralAvionics pm, ModuleFuelTanks rfPM, FuelTank ecTank, float extraVolumeLiters, float ecAmount)
        {
            float utilizationPercent = rfPM.utilization;
            float utilization = utilizationPercent / 100;
            float avVolume = pm.GetAvionicsVolume();

            // The amount of final available volume that RF tanks get is calculated in 3 steps:
            // 1) ModuleProceduralAvionics.GetAvailableVolume()
            // 2) RF tank's utilization (the slider in the PAW)
            // 3) RF (internal) tank's per-resource utilization value.
            //    This is currently set at 1000 for EC which means that 1l of volume can hold 1000 units of EC.
            // The code below runs all these but in reversed order.

            float lVolStep3 = ecAmount / ecTank.utilization;
            float lVolStep2 = (lVolStep3 + extraVolumeLiters) / utilization;
            lVolStep2 = Math.Max(lVolStep2, pm.CurrentProceduralAvionicsTechNode.reservedRFTankVolume);
            float m3VolStep2 = lVolStep2 / 1000;    // RF volumes are in liters but avionics uses m³
            float m3TotalVolume = Math.Max(avVolume + m3VolStep2, m3VolStep2);
            return m3TotalVolume;
        }

        private float GetECAmountForVolume(ModuleFuelTanks rfPM, FuelTank ecTank, float m3Volume)
        {
            float utilizationPercent = rfPM.utilization;
            float utilization = utilizationPercent / 100;

            float step3 = m3Volume * 1000 * utilization;
            float ecAmount = step3 * ecTank.utilization;
            return ecAmount;
        }

        private string GetTooltipTextForTechNode(ProceduralAvionicsTechNode techNode)
        {
            if (!_tooltipTexts.TryGetValue(techNode.name, out string tooltip))
            {
                tooltip = ConstructTooltipForAvionicsTL(techNode);
                _tooltipTexts[techNode.name] = tooltip;
            }

            return tooltip;
        }

        private string BuildCostString(double cost, double baseCost) =>
            (baseCost == 0 || HighLogic.CurrentGame.Parameters.Difficulty.BypassEntryPurchaseAfterResearch) ? string.Empty : $"{cost:N0}";

        private string ConstructTooltipForAvionicsTL(ProceduralAvionicsTechNode techNode)
        {
            var sb = StringBuilderCache.Acquire();
            if (!techNode.IsAvailable)
            {
                sb.AppendLine($"<color=orange>This tech level can be used in simulations but will prevent the vessel from being built until {techNode.TechNodeTitle} has been researched.</color>\n");
            }

            float calcMass = ModuleProceduralAvionics.GetStatsForTechNode(techNode, _newControlMass, out float massKG, out _, out float powerWatts);
            string indent = string.Empty;
            if (!techNode.IsScienceCore)
            {
                sb.AppendLine($"At {calcMass:0.##}t controllable mass:");
                indent = "  ";
            }
            sb.AppendLine($"{indent}Avionics mass: {massKG:0.#}kg");
            sb.AppendLine($"{indent}Power consumption: {powerWatts:0.#}W");

            sb.AppendLine($"Axial control: {BoolToYesNoString(techNode.allowAxial)}");
            sb.AppendLine($"Can hibernate: {BoolToYesNoString(techNode.disabledPowerFactor > 0)}");
            sb.Append($"Sample container: {BoolToYesNoString(techNode.hasScienceContainer)}");

            return sb.ToStringAndRelease();
        }

        private static void FetchRFModule(ModuleAvionics pm, out ModuleFuelTanks rfPM, out FuelTank ecTank)
        {
            rfPM = pm.part.FindModuleImplementing<ModuleFuelTanks>();
            FieldInfo fiDict = typeof(ModuleFuelTanks).GetField("tanksDict", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            var tanksDict = (Dictionary<string, FuelTank>)fiDict.GetValue(rfPM);
            ecTank = tanksDict["ElectricCharge"];
        }

        private static string BoolToYesNoString(bool b) => b ? "Yes" : "No";

        private void RenderAvionicsTypeVisualization()
        {
            // Calculate volume components
            float avionicsVolume = _module.GetAvionicsVolume();
            float rfTotalVolume = (float)_rfPM.totalVolume / 1000f; // Total RF tank volume in m³
            float totalVolume = avionicsVolume + rfTotalVolume; // Total part volume
            float utilization = _module.Utilization;
            
            // Calculate mass components (in kg)
            float avionicsMass = _module.GetModuleMass(_module.GetAvionicsVolume(), ModifierStagingSituation.CURRENT) * 1000f; // Convert tons to kg
            float rfTankMass = _rfPM.mass * 1000f; // RF tank total mass in kg (includes structure + all resources)
            float totalMass = avionicsMass + rfTankMass; // Total mass in kg

            // Get current config info - use the module's actual config, not the selected tab
            string configName = _module.avionicsConfigName;
            ProceduralAvionicsConfig config = ProceduralAvionicsTechManager.GetProceduralAvionicsConfig(configName);
            string techLevelName = _module.CurrentProceduralAvionicsTechNode?.dispName ?? _module.CurrentProceduralAvionicsTechNode?.name ?? "Unknown";

            // Dark background card
            RP0.UI.SharedUIComponents.BeginCard("");
            
            GUILayout.Space(4);

            // Title row with config name, tech level, and toggle button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var configStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = config.IsAvailable ? RP0.UI.SharedUIComponents.Colors.TextPrimary : new Color(1f, 0.65f, 0.3f) },
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"{configName} - {techLevelName}", configStyle);
            GUILayout.FlexibleSpace();
            
            // Toggle button for Volume/Mass view
            _gc ??= new GUIContent();
            _gc.text = _showMassBreakdown ? "kg" : "L";
            _gc.tooltip = _showMassBreakdown ? "Showing mass breakdown - click to show volume" : "Showing volume breakdown - click to show mass";
            if (GUILayout.Button(_gc, HighLogic.Skin.button, GUILayout.Width(35), GUILayout.Height(24)))
            {
                _showMassBreakdown = !_showMassBreakdown;
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(18);

            // Horizontal bar visualization with indicators
            float barWidth = 500f;
            float barHeight = 25f;
            float indicatorHeight = 20f;
            float totalHeight = 120f; // Increased space for larger fonts and more breathing room
            
            Rect visualRect = GUILayoutUtility.GetRect(barWidth + 100, totalHeight);
            
            if (Event.current.type == EventType.Repaint && (_showMassBreakdown ? totalMass : totalVolume) > 0)
            {
                // Calculate segment widths based on current mode
                float total = _showMassBreakdown ? totalMass : totalVolume;
                float avionicsValue = _showMassBreakdown ? avionicsMass : avionicsVolume;
                float rfValue = _showMassBreakdown ? rfTankMass : rfTotalVolume;
                
                float avionicsWidth = (avionicsValue / total) * barWidth;
                float rfWidth = (rfValue / total) * barWidth;
                
                float barX = visualRect.x + 50;
                float barY = visualRect.y + 35; // Center vertically with space for indicators
                float currentX = barX;
                
                // Define bar and indicator rectangles for hover detection
                Rect avionicsBarRect = new Rect(barX, barY, avionicsWidth, barHeight);
                Rect rfBarRect = new Rect(barX + avionicsWidth, barY, rfWidth, barHeight);
                
                float avionicsCenterX = barX + avionicsWidth / 2;
                float rfCenterX = barX + avionicsWidth + rfWidth / 2;
                
                Rect avionicsIndicatorRect = new Rect(avionicsCenterX - 60, barY + barHeight + indicatorHeight, 120, 36);
                Rect rfIndicatorRect = new Rect(rfCenterX - 60, barY - indicatorHeight - 32, 120, 36);
                
                // Detect hover over bars or indicators
                bool hoverAvionics = avionicsBarRect.Contains(Event.current.mousePosition) || avionicsIndicatorRect.Contains(Event.current.mousePosition);
                bool hoverRF = rfBarRect.Contains(Event.current.mousePosition) || rfIndicatorRect.Contains(Event.current.mousePosition);
                
                currentX = barX;
                
                // Draw avionics segment (blue) - left
                if (avionicsWidth > 1)
                {
                    Color avionicsColor = hoverAvionics ? new Color(0.4f, 0.6f, 1.0f, 1f) : new Color(0.3f, 0.5f, 0.9f, 1f);
                    var avionicsTex = RP0.UI.SharedUIComponents.MakeTex(2, 2, avionicsColor);
                    GUI.DrawTexture(new Rect(currentX, barY, avionicsWidth, barHeight), avionicsTex);
                    
                    // Invisible label for tooltip over bar
                    _gc ??= new GUIContent();
                    _gc.text = "";
                    if (_showMassBreakdown)
                    {
                        _gc.tooltip = $"Avionics hardware mass: {avionicsMass:F2}kg\nContains guidance, control, and processing equipment";
                    }
                    else
                    {
                        _gc.tooltip = $"Avionics hardware volume: {avionicsVolume * 1000f:F2}L ({avionicsVolume:F4}m³)\nContains guidance, control, and processing equipment";
                    }
                    GUI.Label(new Rect(currentX, barY, avionicsWidth, barHeight), _gc);
                    
                    currentX += avionicsWidth;
                }
                
                // Draw RF tank segment (yellow) - right
                if (rfWidth > 1)
                {
                    Color rfColor = hoverRF ? new Color(1.0f, 0.9f, 0.4f, 1f) : new Color(0.9f, 0.8f, 0.3f, 1f);
                    var rfTex = RP0.UI.SharedUIComponents.MakeTex(2, 2, rfColor);
                    GUI.DrawTexture(new Rect(currentX, barY, rfWidth, barHeight), rfTex);
                    
                    // Invisible label for tooltip over bar
                    _gc ??= new GUIContent();
                    _gc.text = "";
                    if (_showMassBreakdown)
                    {
                        _gc.tooltip = $"RealFuels tank total mass: {rfTankMass:F2}kg\nIncludes tank structure and all resources (EC: {_ecTank.maxAmount:F0} kJ)";
                    }
                    else
                    {
                        _gc.tooltip = $"RealFuels tank total volume: {rfTotalVolume * 1000f:F2}L ({rfTotalVolume:F4}m³)\nContains EC only.";
                    }
                    GUI.Label(new Rect(currentX, barY, rfWidth, barHeight), _gc);
                }
                
                // Draw border around bar
                var borderTex = RP0.UI.SharedUIComponents.MakeTex(2, 2, Color.white);
                GUI.DrawTexture(new Rect(barX - 1, barY - 1, barWidth + 2, 1), borderTex); // Top
                GUI.DrawTexture(new Rect(barX - 1, barY + barHeight, barWidth + 2, 1), borderTex); // Bottom
                GUI.DrawTexture(new Rect(barX - 1, barY - 1, 1, barHeight + 2), borderTex); // Left
                GUI.DrawTexture(new Rect(barX + barWidth, barY - 1, 1, barHeight + 2), borderTex); // Right
                
                // Draw indicators and labels
                var lineTex = RP0.UI.SharedUIComponents.MakeTex(2, 2, new Color(0.7f, 0.7f, 0.7f, 1f));
                
                // Avionics indicator - BELOW bar, pointing UP
                // Draw vertical line pointing up from below
                GUI.DrawTexture(new Rect(avionicsCenterX - 1, barY + barHeight, 2, indicatorHeight), lineTex);
                
                // Draw arrow pointing up
                GUI.DrawTexture(new Rect(avionicsCenterX - 3, barY + barHeight, 6, 3), lineTex, ScaleMode.ScaleToFit);
                
                // Label below
                var avionicsLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = hoverAvionics ? 15 : 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = hoverAvionics ? new Color(0.5f, 0.7f, 1.0f) : new Color(0.4f, 0.6f, 1.0f) },
                    alignment = TextAnchor.MiddleCenter
                };
                
                var avionicsValueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = hoverAvionics ? 14 : 13,
                    normal = { textColor = hoverAvionics ? new Color(0.5f, 0.7f, 1.0f) : new Color(0.4f, 0.6f, 1.0f) },
                    alignment = TextAnchor.MiddleCenter
                };
                
                _gc ??= new GUIContent();
                _gc.text = "Avionics";
                if (_showMassBreakdown)
                {
                    _gc.tooltip = hoverAvionics ? $"Avionics hardware mass: {avionicsMass:F2}kg\nContains guidance, control, and processing equipment" : "";
                }
                else
                {
                    _gc.tooltip = hoverAvionics ? $"Avionics hardware volume: {avionicsVolume * 1000f:F2}L ({avionicsVolume:F4}m³)\nContains guidance, control, and processing equipment" : "";
                }
                GUI.Label(new Rect(avionicsCenterX - 60, barY + barHeight + indicatorHeight, 120, 18), _gc, avionicsLabelStyle);
                string avionicsValueText = _showMassBreakdown ? $"{avionicsMass:F1}kg" : $"{avionicsVolume * 1000f:F1}L";
                GUI.Label(new Rect(avionicsCenterX - 60, barY + barHeight + indicatorHeight + 18, 120, 16), avionicsValueText, avionicsValueStyle);
                
                // RF Tank indicator - ABOVE bar, pointing DOWN
                // Draw vertical line pointing down to bar
                GUI.DrawTexture(new Rect(rfCenterX - 1, barY - indicatorHeight, 2, indicatorHeight), lineTex);
                
                // Draw arrow pointing down
                GUI.DrawTexture(new Rect(rfCenterX - 3, barY - 3, 6, 3), lineTex, ScaleMode.ScaleToFit);
                
                // Label above
                var rfLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = hoverRF ? 15 : 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = hoverRF ? new Color(1.0f, 1.0f, 0.5f) : new Color(0.9f, 0.8f, 0.3f) },
                    alignment = TextAnchor.MiddleCenter
                };
                
                var rfValueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = hoverRF ? 14 : 13,
                    normal = { textColor = hoverRF ? new Color(1.0f, 1.0f, 0.5f) : new Color(0.9f, 0.8f, 0.3f) },
                    alignment = TextAnchor.MiddleCenter
                };
                
                _gc.text = "Battery Tank";
                if (_showMassBreakdown)
                {
                    _gc.tooltip = hoverRF ? $"RealFuels tank total mass: {rfTankMass:F2}kg\nIncludes tank structure and all resources (EC: {_ecTank.maxAmount:F0} kJ)" : "";
                }
                else
                {
                    _gc.tooltip = hoverRF ? $"RealFuels tank total volume: {rfTotalVolume * 1000f:F2}L ({rfTotalVolume:F4}m³)\nContains EC and available space for resources" : "";
                }
                GUI.Label(new Rect(rfCenterX - 60, barY - indicatorHeight - 32, 120, 18), _gc, rfLabelStyle);
                string rfValueText = _showMassBreakdown ? $"{rfTankMass:F1}kg" : $"{rfTotalVolume * 1000f:F1}L";
                GUI.Label(new Rect(rfCenterX - 60, barY - indicatorHeight - 14, 120, 16), rfValueText, rfValueStyle);
            
            }

            RP0.UI.SharedUIComponents.EndCard();
        }

        private void RenderControlInputs()
        {
            RP0.UI.SharedUIComponents.BeginCard("");
            
            GUILayout.Space(8);
            
            // Single row with Control Mass, EC, and Apply buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // Define color-coded styles matching the visualization
            var blueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.5f, 0.9f) }
            };
            
            var yellowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.8f, 0.3f) }
            };
            
            // Control Mass input
            GUILayout.Label("Control Mass:", blueStyle);
            GUILayout.Space(4);
            
            bool isScienceCore = _module.CurrentProceduralAvionicsTechNode?.IsScienceCore ?? false;
            if (isScienceCore)
            {
                ControllableMass = "0";
            }
            
            // Left arrow button for Control Mass
            GUI.enabled = !isScienceCore;
            if (GUILayout.Button("<", HighLogic.Skin.button, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (float.TryParse(ControllableMass, out float val))
                {
                    string oldValue = ControllableMass;
                    float step = Event.current.shift ? 100f : 1f;
                    float newValue = Math.Max(0, val - step);
                    ControllableMass = newValue.ToString("0.###");
                    
                    if (!float.TryParse(_sECAmount, out float ecAmount) || ecAmount <= 0)
                    {
                        ControllableMass = oldValue;
                    }
                    else
                    {
                        ApplyAvionicsSettings(shouldSeekVolume: true);
                    }
                }
            }
            GUI.enabled = true;
            
            float oldControlMass = _newControlMass;
            GUI.enabled = !isScienceCore;
            ControllableMass = GUILayout.TextField(ControllableMass, HighLogic.Skin.textField, GUILayout.Width(50));
            GUI.enabled = true;
            
            // Right arrow button for Control Mass
            GUI.enabled = !isScienceCore;
            if (GUILayout.Button(">", HighLogic.Skin.button, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (float.TryParse(ControllableMass, out float val))
                {
                    string oldValue = ControllableMass;
                    float step = Event.current.shift ? 100f : 1f;
                    float newValue = val + step;
                    ControllableMass = newValue.ToString("0.###");
                    
                    if (!float.TryParse(_sECAmount, out float ecAmount) || ecAmount <= 0)
                    {
                        ControllableMass = oldValue;
                    }
                    else
                    {
                        ApplyAvionicsSettings(shouldSeekVolume: true);
                    }
                }
            }
            GUI.enabled = true;
            
            // Parse the controllable mass here so the table updates
            if (float.TryParse(ControllableMass, out _newControlMass))
            {
                if (oldControlMass != _newControlMass)
                {
                    _tooltipTexts.Clear();
                }
            }
            
            GUILayout.Label("t", blueStyle);
            GUILayout.Space(12);
            
            // EC input
            GUILayout.Label("EC:", yellowStyle);
            GUILayout.Space(4);
            
            // Left arrow button for EC
            if (GUILayout.Button("<", HighLogic.Skin.button, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (float.TryParse(_sECAmount, out float val))
                {
                    string oldValue = _sECAmount;
                    float step = Event.current.shift ? 1000f : 1f;
                    float newValue = Math.Max(1, val - step);
                    _sECAmount = newValue.ToString("F0");
                    
                    if (!float.TryParse(ControllableMass, out float controlMass) || controlMass < 0)
                    {
                        _sECAmount = oldValue;
                    }
                    else
                    {
                        ApplyAvionicsSettings(shouldSeekVolume: true);
                    }
                }
            }
            
            _sECAmount = GUILayout.TextField(_sECAmount, HighLogic.Skin.textField, GUILayout.Width(50));
            
            // Right arrow button for EC
            if (GUILayout.Button(">", HighLogic.Skin.button, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (float.TryParse(_sECAmount, out float val))
                {
                    string oldValue = _sECAmount;
                    float step = Event.current.shift ? 1000f : 1f;
                    float newValue = val + step;
                    _sECAmount = newValue.ToString("F0");
                    
                    if (!float.TryParse(ControllableMass, out float controlMass) || controlMass < 0)
                    {
                        _sECAmount = oldValue;
                    }
                    else
                    {
                        ApplyAvionicsSettings(shouldSeekVolume: true);
                    }
                }
            }
            
            GUILayout.Label("kJ", yellowStyle);
            GUILayout.Space(12);
            
            // Apply buttons inline
            _gc ??= new GUIContent();
            _gc.text = "Apply (Fit)";
            _gc.tooltip = "Applies the parameters and resizes the part to have the optimal amount of volume";
            if (GUILayout.Button(_gc, HighLogic.Skin.button, GUILayout.Width(90), GUILayout.Height(24)))
            {
                ApplyAvionicsSettings(shouldSeekVolume: true);
            }

            GUILayout.Space(4);

            _gc.text = "Apply (Preserve)";
            _gc.tooltip = "Applies the parameters but doesn't resize the part even if there isn't enough volume";
            if (GUILayout.Button(_gc, HighLogic.Skin.button, GUILayout.Width(110), GUILayout.Height(24)))
            {
                ApplyAvionicsSettings(shouldSeekVolume: false);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // EC Drain Time Display
            
            if (float.TryParse(_sECAmount, out float displayECAmount) && displayECAmount > 0 && 
                _module.CurrentProceduralAvionicsTechNode != null)
            {
                ModuleProceduralAvionics.GetStatsForTechNode(_module.CurrentProceduralAvionicsTechNode, _newControlMass, out _, out _, out float currentPowerWatts);
                float idlePowerWatts = currentPowerWatts * _module.CurrentProceduralAvionicsTechNode.disabledPowerFactor;
                
                // Convert kJ to J for calculations
                float ecJoules = displayECAmount * 1000f;
                
                // Calculate drain time in seconds
                float activeDrainSeconds = currentPowerWatts > 0 ? ecJoules / currentPowerWatts : 0;
                float idleDrainSeconds = idlePowerWatts > 0 ? ecJoules / idlePowerWatts : 0;
                
                // Helper function to format time nicely
                string FormatDrainTime(float seconds)
                {
                    if (seconds <= 0) return "∞";
                    if (seconds < 60) return $"{seconds:F1}s";
                    if (seconds < 3600) return $"{seconds / 60:F1}m";
                    if (seconds < 86400) return $"{seconds / 3600:F1}h";
                    return $"{seconds / 86400:F1}d";
                }
                
                Rect drainTimeRowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(18));
                bool isHoveringDrainTime = drainTimeRowRect.Contains(Event.current.mousePosition);
                
                var drainStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = isHoveringDrainTime ? 14 : 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = RP0.UI.SharedUIComponents.Colors.TextSecondary },
                    wordWrap = false,
                    alignment = TextAnchor.MiddleCenter
                };
                
                var activeStyle = new GUIStyle(drainStyle)
                {
                    normal = { textColor = new Color(1f, 0.7f, 0.7f) } // Light red for active
                };
                
                var idleStyle = new GUIStyle(drainStyle)
                {
                    normal = { textColor = new Color(1f, 1f, 0.7f) } // Light yellow for idle
                };
                
                _gc ??= new GUIContent();
                _gc.tooltip = "Estimated time to completely drain this EC storage with no other power sources or EC storage. Given only this avionics module drawing power.";
                
                // Build the full text string to measure total width
                string labelText = "EC drain time: ";
                string activeText = currentPowerWatts > 0 ? $"Active: {FormatDrainTime(activeDrainSeconds)}" : "";
                string idleTimeText = idlePowerWatts > 0 ? FormatDrainTime(idleDrainSeconds) : "-";
                string idleText = $"Idle: {idleTimeText}";
                
                // Calculate widths
                _gc.text = labelText;
                float labelWidth = drainStyle.CalcSize(_gc).x;
                
                _gc.text = activeText;
                float activeWidth = currentPowerWatts > 0 ? activeStyle.CalcSize(_gc).x : 0;
                
                _gc.text = idleText;
                float idleWidth = idleStyle.CalcSize(_gc).x;
                
                float spacing = 20f;
                float totalWidth = labelWidth + (currentPowerWatts > 0 ? activeWidth + spacing : 0) + spacing + idleWidth;
                
                // Center horizontally
                float startX = drainTimeRowRect.x + (drainTimeRowRect.width - totalWidth) / 2f;
                float currentX = startX;
                
                // Draw label
                _gc.text = labelText;
                GUI.Label(new Rect(currentX, drainTimeRowRect.y, labelWidth, drainTimeRowRect.height), _gc, drainStyle);
                currentX += labelWidth;
                
                // Draw active time
                if (currentPowerWatts > 0)
                {
                    _gc.text = activeText;
                    GUI.Label(new Rect(currentX, drainTimeRowRect.y, activeWidth, drainTimeRowRect.height), _gc, activeStyle);
                    currentX += activeWidth + spacing;
                }
                else
                {
                    currentX += spacing;
                }
                
                // Draw idle time
                _gc.text = idleText;
                GUI.Label(new Rect(currentX, drainTimeRowRect.y, idleWidth, drainTimeRowRect.height), _gc, idleStyle);
            }
            
            GUILayout.Space(4);
            
            // Warning messages
            if (_showROTankSizeWarning || _showSizeWarning)
            {
                GUILayout.Space(2);
                var warningStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = RP0.UI.SharedUIComponents.Colors.Warning },
                    fontSize = 13,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                
                if (_showROTankSizeWarning)
                    GUILayout.Label("ROTanks does not support automatic resizing. Increase part size manually.", warningStyle);
                else if (_showSizeWarning)
                    GUILayout.Label("Not enough volume. Increase part size manually.", warningStyle);
            }

            RP0.UI.SharedUIComponents.EndCard();
            
            GUI.DragWindow();
            
            Tooltip.Instance.RecordTooltip(_windowId);
        }
    }
}
