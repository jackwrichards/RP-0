using System;
using System.Collections.Generic;
using UnityEngine;
using RP0.UI.Budget;
using RP0.UI;

namespace RP0
{
    /// <summary>
    /// Modern budget and maintenance UI with visual dashboards
    /// </summary>
    public class MaintenanceGUI : UIBase
    {
        // Legacy enum for backward compatibility with MaintenanceHandler
        public enum MaintenancePeriod { Day, Month, Year };

        private BudgetPeriod _selectedPeriod = BudgetPeriod.Month;
        private BudgetSnapshot _currentSnapshot;
        private List<BudgetAlert> _alerts;
        private string _warpToFundsString = string.Empty;

        // Tab-specific state
        private Vector2 _nautListScroll = Vector2.zero;
        private Vector2 _alertsScroll = Vector2.zero;
        private Dictionary<string, string> _siteLocalizer = new Dictionary<string, string>();
        
        // Chart time range selection
        private int _chartMonthsToShow = 120; // Default: 10 years (120 months)
        
        // View toggle for compact/full view
        private bool _compactView = false;

        protected override void OnStart()
        {
            base.OnStart();
            BudgetUIComponents.InitializeStyles();
        }

        /// <summary>
        /// Refresh all budget calculations - called automatically on every render
        /// </summary>
        private void RefreshBudgetData()
        {
            if (HighLogic.CurrentGame?.Mode != Game.Modes.CAREER)
                return;

            _currentSnapshot = BudgetCalculator.Instance.GenerateSnapshot(_selectedPeriod);
            _alerts = BudgetCalculator.Instance.GenerateAlerts(_currentSnapshot);
        }

        /// <summary>
        /// Main budget summary dashboard
        /// </summary>
        public void RenderSummaryTab()
        {
            if (SpaceCenterManagement.Instance == null)
                return;

            // Always refresh data to keep it live
            RefreshBudgetData();

            // Period selector (left), View toggle, Warp button (middle), Chart range (right)
            GUILayout.BeginHorizontal();
            
            BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
            if (newPeriod != _selectedPeriod)
            {
                _selectedPeriod = newPeriod;
            }
            
            GUILayout.Space(8);
            
            // View toggle button
            var pressedStyle = new GUIStyle(HighLogic.Skin.button);
            pressedStyle.normal = pressedStyle.active;
            
            if (GUILayout.Button(_compactView ? "Compact" : "Full", _compactView ? pressedStyle : HighLogic.Skin.button, GUILayout.Height(24)))
            {
                _compactView = !_compactView;
                TopWindow.RequestUIReset(); // Resize window when toggling view
            }
            
            GUILayout.FlexibleSpace();
            
            // Warp to Fund Target button in the middle
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                if (GUILayout.Button("Warp", HighLogic.Skin.button, GUILayout.Height(24)))
                {
                    ShowWarpToFundsDlg();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            // Chart range selector on the right with subtle label (only in full view)
            if (!_compactView && CareerLog.Instance != null && CareerLog.Instance.IsEnabled)
            {
                _chartMonthsToShow = SharedUIComponents.RenderChartRangeSelector(_chartMonthsToShow);
            }
            
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (_compactView)
            {
                // Compact view: Only show Current Funds, Unlock Credit, and Budget Summary
                GUILayout.BeginHorizontal();
                
                // Current Funds
                BudgetUIComponents.RenderMetricCard(
                    "Current Funds",
                    $"√{_currentSnapshot.CurrentFunds:N0}",
                    BudgetUIComponents.Colors.Accent,
                    KSPUtil.PrintDate(_currentSnapshot.Timestamp, false)
                );

                GUILayout.Space(4);

                // Unlock Credit
                double unlockCreditValue = UnlockCreditHandler.Instance?.TotalCredit ?? 0d;
                double unlockCreditRate = CurrencyUtils.Rate(TransactionReasonsRP0.RateUnlockCreditIncrease);
                string unlockCreditTooltip = "Unlock credit is earned from paying your research teams (35% of their salaries). " +
                                            "It reduces the cost of unlocking new parts, part upgrades, and tooling, " +
                                            "simulating that R&D costs are already covered by your research budget.";
                BudgetUIComponents.RenderMetricCard(
                    "Unlock Credit",
                    $"√{unlockCreditValue:N0}",
                    new Color(1.0f, 0.9f, 0.3f), // Yellow
                    $"Rate: {unlockCreditRate:F2}x",
                    unlockCreditTooltip
                );
                
                GUILayout.EndHorizontal();
                
                GUILayout.Space(4);
                
                // Budget summary - constrained to match width of two metric cards above (130 + 4 + 130 = 264)
                GUILayout.BeginVertical(GUILayout.Width(264));
                BudgetUIComponents.RenderBudgetSummary(_currentSnapshot);
                GUILayout.EndVertical();
            }
            else
            {
                // Full view: Show everything
                // Top metrics row
                RenderMetricsRow();

                GUILayout.Space(4);

                // Two-column layout: Budget summary on left, Alerts on right
                GUILayout.BeginHorizontal();
                
                // Left column: Budget summary
                GUILayout.BeginVertical(GUILayout.Width(300));
                BudgetUIComponents.RenderBudgetSummary(_currentSnapshot);
                GUILayout.EndVertical();

                GUILayout.Space(8);

                // Right column: Alerts (including funds runway)
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                
                // Always show alerts section (includes funds runway if negative cash flow)
                RenderAlertsSection();
                
                GUILayout.EndVertical();
                
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                // Historical charts
                if (CareerLog.Instance != null && CareerLog.Instance.IsEnabled)
                {
                    // Funds and Subsidy charts side by side
                    GUILayout.BeginHorizontal();
                    
                    GUILayout.BeginVertical();
                    BudgetUIComponents.BeginCard("Funds & Unlock Credit (Monthly)");
                    BudgetChartRenderer.RenderHistoricalFundsChart(_chartMonthsToShow, 280, 100);
                    BudgetUIComponents.EndCard();
                    GUILayout.EndVertical();

                    GUILayout.Space(8);

                    GUILayout.BeginVertical();
                    BudgetUIComponents.BeginCard("Personnel Count (Monthly)");
                    BudgetChartRenderer.RenderHistoricalPersonnelChart(_chartMonthsToShow, 280, 100);
                    BudgetUIComponents.EndCard();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    // Confidence and Reputation chart
                    BudgetUIComponents.BeginCard("Confidence & Reputation (Monthly)");
                    BudgetChartRenderer.RenderHistoricalConfidenceRepChart(_chartMonthsToShow, 580, 200);
                    BudgetUIComponents.EndCard();
                    GUILayout.Space(4);
                }
            }
        }

        /// <summary>
        /// Render top-level metrics cards
        /// </summary>
        private void RenderMetricsRow()
        {
            GUILayout.BeginHorizontal();

            // Current Funds
            BudgetUIComponents.RenderMetricCard(
                "Current Funds",
                $"√{_currentSnapshot.CurrentFunds:N0}",
                BudgetUIComponents.Colors.Accent,
                KSPUtil.PrintDate(_currentSnapshot.Timestamp, false)
            );

            GUILayout.Space(4);

            // Unlock Credit - show total accumulated, not projected earnings
            double unlockCreditValue = UnlockCreditHandler.Instance?.TotalCredit ?? 0d;
            double unlockCreditRate = CurrencyUtils.Rate(TransactionReasonsRP0.RateUnlockCreditIncrease);
            string unlockCreditTooltip = "Unlock credit is earned from paying your research teams (35% of their salaries). " +
                                        "It reduces the cost of unlocking new parts, part upgrades, and tooling, " +
                                        "simulating that R&D costs are already covered by your research budget.";
            BudgetUIComponents.RenderMetricCard(
                "Unlock Credit",
                $"√{unlockCreditValue:N0}",
                new Color(1.0f, 0.9f, 0.3f), // Yellow
                $"Rate: {unlockCreditRate:F2}x",
                unlockCreditTooltip
            );

            GUILayout.Space(4);

            // Total Income
            BudgetUIComponents.RenderMetricCard(
                "Income",
                BudgetCalculator.FormatCurrency(_currentSnapshot.TotalIncome, "N0"),
                BudgetUIComponents.Colors.Income
            );

            GUILayout.Space(4);

            // Total Expenses
            BudgetUIComponents.RenderMetricCard(
                "Expenses",
                BudgetCalculator.FormatCurrency(_currentSnapshot.TotalExpenses, "N0"),
                BudgetUIComponents.Colors.Expense
            );

            GUILayout.Space(4);

            // Net Cash Flow
            Color netColor = _currentSnapshot.NetCashFlow >= 0 ?
                BudgetUIComponents.Colors.Positive : BudgetUIComponents.Colors.Negative;
            BudgetUIComponents.RenderMetricCard(
                "Net Cash Flow",
                BudgetCalculator.FormatCurrency(_currentSnapshot.NetCashFlow, "N0"),
                netColor,
                _currentSnapshot.NetCashFlow >= 0 ? "Surplus" : "Deficit"
            );

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Render alerts and warnings (including funds runway)
        /// </summary>
        private void RenderAlertsSection()
        {
            BudgetUIComponents.BeginCard("Alerts & Notifications");

            // Scrollable alerts section - height matches budget summary
            _alertsScroll = GUILayout.BeginScrollView(_alertsScroll, GUILayout.ExpandHeight(true));

            // Always show funds runway alert first if negative cash flow
            double dailyCashFlow = _currentSnapshot.NetCashFlow / BudgetCalculator.Instance.GetPeriodMultiplier(_selectedPeriod);
            if (dailyCashFlow < 0)
            {
                double daysRemaining = _currentSnapshot.CurrentFunds / Math.Abs(dailyCashFlow);
                
                BudgetAlert.AlertLevel runwayLevel;
                if (daysRemaining < 30)
                    runwayLevel = BudgetAlert.AlertLevel.Critical;
                else if (daysRemaining < 90)
                    runwayLevel = BudgetAlert.AlertLevel.Warning;
                else
                    runwayLevel = BudgetAlert.AlertLevel.Info;

                var runwayAlert = new BudgetAlert(
                    $"Funds runway: {KSPUtil.PrintDateDeltaCompact(daysRemaining * 86400, false, false)}",
                    runwayLevel,
                    "Funds Runway"
                );
                BudgetUIComponents.RenderAlert(runwayAlert);
            }

            // Show other alerts
            if (_alerts != null && _alerts.Count > 0)
            {
                foreach (var alert in _alerts)
                {
                    BudgetUIComponents.RenderAlert(alert);
                }
            }

            // Show message if no alerts
            if (dailyCashFlow >= 0 && (_alerts == null || _alerts.Count == 0))
            {
                var noAlertsStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = BudgetUIComponents.Colors.TextSecondary },
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("No alerts - all systems nominal", noAlertsStyle);
            }

            GUILayout.EndScrollView();

            BudgetUIComponents.EndCard();
        }


        /// <summary>
        /// Show warp to funds dialog
        /// </summary>
        private void ShowWarpToFundsDlg()
        {
            InputLockManager.SetControlLock(ControlTypes.KSC_ALL, "warptofunds");
            UIHolder.Instance.HideWindow();

            if (SpaceCenterManagement.Instance.staffTarget.IsValid)
            {
                string msg = "This functionality cannot be used while there's automatic staff hiring in progress.";
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("warpToFunds", msg, "Warp To Funds", HighLogic.UISkin,
                        new DialogGUIButton("Understood", () =>
                        {
                            UIHolder.Instance.ShowWindow();
                            InputLockManager.RemoveControlLock("warptofunds");
                        })
                    ), false, HighLogic.UISkin);
            }
            else
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("warpToFunds", "Fund Target", "Warp To Funds", HighLogic.UISkin,
                        new DialogGUITextInput(_warpToFundsString, false, 64, (string n) =>
                        {
                            _warpToFundsString = n;
                            return _warpToFundsString;
                        }, 24f),
                        new DialogGUIButton("Estimate Time", () => { ShowConfirmWarpDialog(); }),
                        new DialogGUIButton("Cancel", () =>
                        {
                            UIHolder.Instance.ShowWindow();
                            InputLockManager.RemoveControlLock("warptofunds");
                        })
                    ), false, HighLogic.UISkin);
            }
        }

        /// <summary>
        /// Show warp confirmation dialog
        /// </summary>
        private void ShowConfirmWarpDialog()
        {
            if (!double.TryParse(_warpToFundsString, out double fundTarget))
            {
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog("warpToFundsConfirmFail",
                    "Failed to parse funds!",
                    "Error",
                    HighLogic.UISkin,
                    300,
                    new DialogGUIButton("Understood", () => {
                        UIHolder.Instance.ShowWindow();
                        InputLockManager.RemoveControlLock("warptofunds");
                    })), false, HighLogic.UISkin);
                return;
            }

            var forecast = BudgetCalculator.Instance.CalculateForecast(fundTarget, _currentSnapshot);

            if (!forecast.IsAchievable)
            {
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog("warpToFundsConfirmFail",
                    $"Cannot reach target funds. {forecast.Reason}",
                    "Error",
                    HighLogic.UISkin,
                    300,
                    new DialogGUIButton("Understood", () => {
                        UIHolder.Instance.ShowWindow();
                        InputLockManager.RemoveControlLock("warptofunds");
                    })), false, HighLogic.UISkin);
                return;
            }

            if (forecast.TimeToTarget == 0)
            {
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog("warpToFundsConfirmAtFunds",
                    "Already at this funding!",
                    "No Warp Needed",
                    HighLogic.UISkin,
                    300,
                    new DialogGUIButton("Understood", () =>
                    {
                        UIHolder.Instance.ShowWindow();
                        InputLockManager.RemoveControlLock("warptofunds");
                        SpaceCenterManagement.Instance.fundTarget.Clear();
                    })), false, HighLogic.UISkin);
                return;
            }

            FundTargetProject target = new FundTargetProject(fundTarget);
            var options = new DialogGUIBase[] {
                new DialogGUIButton("Yes, Warp", () =>
                {
                    SpaceCenterManagement.Instance.fundTarget.Clear();
                    KCTWarpController.Create(target);
                    UIHolder.Instance.ShowWindow();
                    InputLockManager.RemoveControlLock("warptofunds");
                }),
                new DialogGUIButton("Add Warp Target", () =>
                {
                    SpaceCenterManagement.Instance.fundTarget = target;
                    target.SetAutoWarp(false);
                    UIHolder.Instance.ShowWindow();
                    InputLockManager.RemoveControlLock("warptofunds");
                }),
                new DialogGUIButton("Cancel", () =>
                {
                    SpaceCenterManagement.Instance.fundTarget.Clear();
                    UIHolder.Instance.ShowWindow();
                    InputLockManager.RemoveControlLock("warptofunds");
                })
            };

            var dialog = new MultiOptionDialog("warpToFundsConfirm", 
                $"Warp? Estimated to take {KSPUtil.PrintDateDelta(forecast.TimeToTarget, false, false)} and finish on {KSPUtil.PrintDate(forecast.DateAtTarget, false)}", 
                "Confirm Warp", HighLogic.UISkin, 300, options);
            PopupDialog.SpawnPopupDialog(dialog, false, HighLogic.UISkin);
        }

        #region Legacy Tab Methods (Simplified with new system)

        public void RenderFacilitiesTab()
        {
            if (SpaceCenterManagement.Instance == null)
                return;

            // Always refresh data to keep it live
            RefreshBudgetData();

            BudgetUIComponents.RenderSectionHeader($"Facilities Costs (per {_selectedPeriod})");
            BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
            if (newPeriod != _selectedPeriod)
            {
                _selectedPeriod = newPeriod;
            }

            GUILayout.Space(4);

            var facilityExpenses = _currentSnapshot.Expenses.Find(e => e.Category == BudgetCategory.Facilities);
            if (facilityExpenses != null)
            {
                BudgetUIComponents.BeginCard();
                facilityExpenses.IsExpanded = true;
                BudgetUIComponents.RenderLineItem(facilityExpenses, 0, true);
                BudgetUIComponents.EndCard();
            }
        }

        public void RenderIntegrationTab()
        {
            if (SpaceCenterManagement.Instance == null)
                return;

            // Always refresh data to keep it live
            RefreshBudgetData();

            BudgetUIComponents.RenderSectionHeader($"Integration Teams Cost (per {_selectedPeriod})");
            BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
            if (newPeriod != _selectedPeriod)
            {
                _selectedPeriod = newPeriod;
            }

            GUILayout.Space(4);

            var personnelExpenses = _currentSnapshot.Expenses.Find(e => e.Name == "Integration Teams");
            if (personnelExpenses != null)
            {
                BudgetUIComponents.BeginCard();
                personnelExpenses.IsExpanded = true;
                BudgetUIComponents.RenderLineItem(personnelExpenses, 0, true);
                BudgetUIComponents.EndCard();
            }
        }

        public void RenderConstructionTab()
        {
            if (SpaceCenterManagement.Instance == null)
                return;

            // Always refresh data to keep it live
            RefreshBudgetData();

            BudgetUIComponents.RenderSectionHeader($"Construction Cost (per {_selectedPeriod})");
            BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
            if (newPeriod != _selectedPeriod)
            {
                _selectedPeriod = newPeriod;
            }

            GUILayout.Space(4);

            var constructionExpenses = _currentSnapshot.Expenses.Find(e => e.Name == "Construction Projects");
            if (constructionExpenses != null)
            {
                BudgetUIComponents.BeginCard();
                constructionExpenses.IsExpanded = true;
                BudgetUIComponents.RenderLineItem(constructionExpenses, 0, true);
                BudgetUIComponents.EndCard();
            }
        }

        public void RenderProgramTab()
        {
            if (Programs.ProgramHandler.Instance == null)
                return;

            // Always refresh data to keep it live
            RefreshBudgetData();

            BudgetUIComponents.RenderSectionHeader($"Active Programs (per {_selectedPeriod})");
            BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
            if (newPeriod != _selectedPeriod)
            {
                _selectedPeriod = newPeriod;
            }

            GUILayout.Space(4);

            BudgetUIComponents.BeginCard();
            
            foreach (var item in _currentSnapshot.Income)
            {
                if (item.Category == BudgetCategory.Programs)
                {
                    BudgetUIComponents.RenderLineItem(item, 0, true);
                }
            }

            BudgetUIComponents.EndCard();
        }

        public void RenderAstronautsTab()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                // Always refresh data to keep it live
                RefreshBudgetData();
                
                BudgetUIComponents.RenderSectionHeader($"Astronaut Costs (per {_selectedPeriod})");
                BudgetPeriod newPeriod = BudgetUIComponents.RenderPeriodSelector(_selectedPeriod);
                if (newPeriod != _selectedPeriod)
                {
                    _selectedPeriod = newPeriod;
                }

                GUILayout.Space(4);
            }

            // Astronaut list
            BudgetUIComponents.BeginCard("Astronaut Corps");
            
            GUILayout.BeginHorizontal();
            int nautCount = HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount();
            GUILayout.Label($"{nautCount:N0} astronauts", BoldLabel);
            GUILayout.EndHorizontal();

            _nautListScroll = GUILayout.BeginScrollView(_nautListScroll, GUILayout.Width(320), GUILayout.Height(280));
            RenderNautList();
            GUILayout.EndScrollView();

            BudgetUIComponents.EndCard();

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                GUILayout.Space(4);
                
                var astronautExpenses = _currentSnapshot.Expenses.Find(e => e.Name == "Astronaut Corps");
                if (astronautExpenses != null)
                {
                    BudgetUIComponents.BeginCard("Cost Breakdown");
                    astronautExpenses.IsExpanded = true;
                    BudgetUIComponents.RenderLineItem(astronautExpenses, 0, true);
                    BudgetUIComponents.EndCard();
                }
            }
        }

        private void RenderNautList()
        {
            double periodMultiplier = BudgetCalculator.Instance.GetPeriodMultiplier(_selectedPeriod);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Name", BoldLabel, GUILayout.Width(144));
            GUILayout.Label("Retires NET", BoldLabel, GUILayout.Width(120));
            GUILayout.Label("Upkeep", BoldLabel, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            for (int i = 0; i < HighLogic.CurrentGame.CrewRoster.Count; ++i)
            {
                var k = HighLogic.CurrentGame.CrewRoster[i];
                if (k.rosterStatus == ProtoCrewMember.RosterStatus.Dead || 
                    k.rosterStatus == ProtoCrewMember.RosterStatus.Missing ||
                    k.type != ProtoCrewMember.KerbalType.Crew)
                    continue;

                double rt = Crew.CrewHandler.Instance.GetRetireTime(k.name);
                if (rt == 0d)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(k.displayName, HighLogic.Skin.label, GUILayout.Width(144));
                GUILayout.Label(Crew.CrewHandler.Instance.RetirementEnabled ? KSPUtil.PrintDate(rt, false) : "(n/a)", 
                    HighLogic.Skin.label, GUILayout.Width(120));
                
                MaintenanceHandler.Instance.GetNautCost(k, out double cost, out double flightCost);
                cost += flightCost;
                cost = CurrencyUtils.Funds(TransactionReasonsRP0.SalaryCrew, -cost * periodMultiplier);
                GUILayout.Label(BudgetCalculator.FormatCurrency(cost), RightLabel, GUILayout.Width(50));
                
                GUILayout.EndHorizontal();
            }
        }

        #endregion
    }
}
