using ClickThroughFix;
using System;
using UnityEngine;
using RP0.UI.Budget;
using RP0.UI.Construction;
using RP0.UI.Science;
using RP0.UI.Crew;

namespace RP0
{
    public class TopWindow : UIBase
    {
        private static Rect _windowPos = new Rect(500, 240, 1000, 900);
        private static readonly int _mainWindowId = "RP0Top".GetHashCode();
        private static UITab _currentTab;
        private static bool _shouldResetUISize;

        private readonly MaintenanceGUI _maintUI = new MaintenanceGUI();
        private readonly ToolingGUI _toolUI = new ToolingGUI();
        private readonly AvionicsGUI _avUI = new AvionicsGUI();
        private readonly ContractGUI _contractUI = new ContractGUI();
        private readonly ScienceGUI _scienceUI = new ScienceGUI();
        private readonly CrewGUI _crewUI = new CrewGUI();
        private readonly ConstructionGUI _constructionUI = new ConstructionGUI();

        public TopWindow()
        {
            // Reset the tab on scene changes
            _currentTab = HighLogic.LoadedSceneIsEditor ? UITab.Tooling : default;
            _shouldResetUISize = true;
        }

        public void OnGUI()
        {
            if (_shouldResetUISize && Event.current.type == EventType.Layout)
            {
                _windowPos.width = 0;
                _windowPos.height = 0;
                _shouldResetUISize = false;
            }
            _windowPos = ClickThruBlocker.GUILayoutWindow(_mainWindowId, _windowPos, DrawWindow, "RP-1", HighLogic.Skin.window);
            Tooltip.Instance.ShowTooltip(_mainWindowId);
        }

        protected override void OnStart()
        {
            _maintUI.Start();
            _toolUI.Start();
            _avUI.Start();
            _contractUI.Start();
            _scienceUI.Start();
            _crewUI.Start();
            _constructionUI.Start();
        }

        protected override void OnDestroy()
        {
            _maintUI.Destroy();
            _toolUI.Destroy();
            _avUI.Destroy();
            _contractUI.Destroy();
            _scienceUI.Destroy();
            _crewUI.Destroy();
            _constructionUI.Destroy();
        }

        public static void SwitchTabTo(UITab newTab)
        {
            if (newTab == _currentTab)
                return;
            _currentTab = newTab;
            _shouldResetUISize = true;
        }

        public static void RequestUIReset()
        {
            _shouldResetUISize = true;
        }

        private void UpdateSelectedTab()
        {
            GUILayout.BeginHorizontal();
            if (ShouldShowTab(UITab.Budget) && RenderToggleButton("Budget", _currentTab == UITab.Budget))
                SwitchTabTo(UITab.Budget);
            if (ShouldShowTab(UITab.Construction) && RenderToggleButton("Construction", _currentTab == UITab.Construction))
                SwitchTabTo(UITab.Construction);
            if (ShouldShowTab(UITab.Science) && RenderToggleButton("Science", _currentTab == UITab.Science))
                SwitchTabTo(UITab.Science);
            if (ShouldShowTab(UITab.Crew) && RenderToggleButton("Crew", _currentTab == UITab.Crew))
                SwitchTabTo(UITab.Crew);
            if (ShouldShowTab(UITab.Tooling) && RenderToggleButton("Tooling", _currentTab == UITab.Tooling))
                SwitchTabTo(UITab.Tooling);
            if (ShouldShowTab(UITab.Avionics) && RenderToggleButton("Avionics", _currentTab == UITab.Avionics))
                SwitchTabTo(UITab.Avionics);
            if (ShouldShowTab(UITab.Contracts) && RenderToggleButton("Settings", _currentTab == UITab.Contracts))
                SwitchTabTo(UITab.Contracts);
            GUILayout.EndHorizontal();
        }

        public void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            try
            {
                UpdateSelectedTab();
                if (ShouldShowTab(_currentTab))
                {
                    switch (_currentTab)
                    {
                        case UITab.Budget:
                            _maintUI.RenderSummaryTab();
                            break;
                        case UITab.Science:
                            _scienceUI.RenderScienceTab();
                            break;
                        case UITab.Crew:
                            _crewUI.RenderCrewTab();
                            break;
                        case UITab.Facilities:
                            _maintUI.RenderFacilitiesTab();
                            break;
                        case UITab.Integration:
                            _maintUI.RenderIntegrationTab();
                            break;
                        case UITab.Construction:
                            _constructionUI.RenderConstructionTab();
                            break;
                        case UITab.Programs:
                            _maintUI.RenderProgramTab();
                            break;
                        case UITab.AstronautCosts:
                            _maintUI.RenderAstronautsTab();
                            break;
                        case UITab.Tooling:
                            SwitchTabTo(_toolUI.RenderToolingTab());
                            break;
                        case UITab.ToolingType:
                            _toolUI.RenderTypeTab();
                            break;
                        case UITab.Avionics:
                            _avUI.RenderAvionicsTab();
                            break;
                        case UITab.Contracts:
                            _contractUI.RenderContractsTab();
                            break;
                        default:    // can't happen
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUI.DragWindow();

            Tooltip.Instance.RecordTooltip(_mainWindowId);
        }
    }
}
