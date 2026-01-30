using System;
using System.Collections.Generic;
using System.Linq;

namespace RP0.UI.Budget
{
    /// <summary>
    /// Handles all budget calculations and data aggregation
    /// </summary>
    public class BudgetCalculator
    {
        private static BudgetCalculator _instance;
        public static BudgetCalculator Instance => _instance ?? (_instance = new BudgetCalculator());

        private BudgetCalculator()
        {
        }

        /// <summary>
        /// Get the multiplier for converting daily costs to the selected period
        /// </summary>
        public double GetPeriodMultiplier(BudgetPeriod period)
        {
            return period switch
            {
                BudgetPeriod.Day => 1,
                BudgetPeriod.Month => 30,
                BudgetPeriod.Year => 365.25,
                _ => 1,
            };
        }

        /// <summary>
        /// Generate a complete budget snapshot for the current game state
        /// </summary>
        public BudgetSnapshot GenerateSnapshot(BudgetPeriod period)
        {
            var snapshot = new BudgetSnapshot
            {
                Period = period,
                CurrentFunds = Funding.Instance?.Funds ?? 0,
                Timestamp = Planetarium.GetUniversalTime()
            };

            double periodMultiplier = GetPeriodMultiplier(period);

            // Calculate all expenses
            snapshot.Expenses.AddRange(CalculateFacilityCosts(periodMultiplier));
            snapshot.Expenses.AddRange(CalculatePersonnelCosts(periodMultiplier));
            snapshot.Expenses.AddRange(CalculateOperationalCosts(periodMultiplier));

            // Calculate income
            snapshot.Income.AddRange(CalculateIncome(periodMultiplier));
            snapshot.Income.AddRange(CalculateProgramCosts(periodMultiplier));

            // Project future funds
            snapshot.ProjectedFunds = snapshot.CurrentFunds + (snapshot.NetCashFlow * periodMultiplier);

            return snapshot;
        }

        /// <summary>
        /// Calculate facility maintenance costs
        /// </summary>
        private List<BudgetLineItem> CalculateFacilityCosts(double periodMultiplier)
        {
            var items = new List<BudgetLineItem>();

            if (SpaceCenterManagement.Instance == null)
                return items;

            var facilityItem = new BudgetLineItem
            {
                Name = "Facilities",
                Category = BudgetCategory.Facilities,
                Description = "Building and launch complex maintenance",
                Icon = ""
            };

            // KSP Facilities
            double kspFacilityCost = 0;
            int facilityCount = 0;
            foreach (var facility in MaintenanceHandler.Instance.FacilitiesForMaintenance)
            {
                if (MaintenanceHandler.Instance.FacilityMaintenanceCosts.TryGetValue(facility, out double cost))
                {
                    double adjustedCost = CurrencyUtils.Funds(TransactionReasonsRP0.StructureRepair, -cost * periodMultiplier);
                    kspFacilityCost += Math.Abs(adjustedCost) * -1; // Ensure negative
                    facilityCount++;

                    facilityItem.SubItems.Add(new BudgetLineItem
                    {
                        Name = ScenarioUpgradeableFacilities.GetFacilityName(facility),
                        Amount = Math.Abs(adjustedCost) * -1, // Ensure negative
                        Category = BudgetCategory.Facilities,
                        Icon = ""
                    });
                }
            }

            // Launch Complexes by site
            double lcTotalCost = 0;
            int lcCount = 0;
            foreach (var ksc in SpaceCenterManagement.Instance.KSCs)
            {
                double siteCost = 0;
                var siteItem = new BudgetLineItem
                {
                    Name = ksc.KSCName,
                    Category = BudgetCategory.Facilities,
                    Icon = ""
                };

                foreach (var lc in ksc.LaunchComplexes)
                {
                    if (!lc.IsOperational)
                        continue;

                    double cost = MaintenanceHandler.Instance.LCUpkeep(lc) * periodMultiplier;
                    cost = CurrencyUtils.Funds(TransactionReasonsRP0.StructureRepairLC, -cost);
                    cost = Math.Abs(cost) * -1; // Ensure negative
                    siteCost += cost;
                    lcCount++;

                    siteItem.SubItems.Add(new BudgetLineItem
                    {
                        Name = lc.Name,
                        Amount = cost,
                        Category = BudgetCategory.Facilities,
                        Icon = ""
                    });
                }

                if (siteCost != 0)
                {
                    siteItem.Amount = siteCost;
                    facilityItem.SubItems.Add(siteItem);
                    lcTotalCost += siteCost;
                }
            }

            facilityItem.Amount = kspFacilityCost + lcTotalCost;
            facilityItem.Count = facilityCount + lcCount;
            facilityItem.CountLabel = "facilities";
            items.Add(facilityItem);

            return items;
        }

        /// <summary>
        /// Calculate personnel costs (engineers, researchers, astronauts)
        /// </summary>
        private List<BudgetLineItem> CalculatePersonnelCosts(double periodMultiplier)
        {
            var items = new List<BudgetLineItem>();

            if (MaintenanceHandler.Instance == null)
                return items;

            // Integration Engineers
            var engineersItem = new BudgetLineItem
            {
                Name = "Integration Teams",
                Category = BudgetCategory.Personnel,
                Description = "Engineer salaries for vehicle integration",
                Icon = ""
            };

            double engineerTotal = 0;
            int totalEngineers = 0;
            foreach (var kvp in MaintenanceHandler.Instance.IntegrationSalaries)
            {
                if (kvp.Value == 0) continue;

                double cost = -kvp.Value * Database.SettingsSC.salaryEngineers * periodMultiplier / 365.25d;
                cost = CurrencyUtils.Funds(TransactionReasonsRP0.SalaryEngineers, cost);
                cost = Math.Abs(cost) * -1; // Ensure negative
                engineerTotal += cost;
                totalEngineers += (int)kvp.Value;

                engineersItem.SubItems.Add(new BudgetLineItem
                {
                    Name = kvp.Key,
                    Amount = cost,
                    Category = BudgetCategory.Personnel,
                    Description = $"{kvp.Value} engineers",
                    Icon = ""
                });
            }
            engineersItem.Amount = engineerTotal;
            engineersItem.Count = totalEngineers;
            engineersItem.CountLabel = "engineers";
            // Calculate cost per engineer
            if (totalEngineers > 0)
                engineersItem.CostPerUnit = engineerTotal / totalEngineers;
            if (engineerTotal != 0)
                items.Add(engineersItem);

            // Research Teams
            double researchCost = CurrencyUtils.Funds(TransactionReasonsRP0.SalaryResearchers,
                -MaintenanceHandler.Instance.ResearchSalaryPerDay * periodMultiplier);
            researchCost = Math.Abs(researchCost) * -1; // Ensure negative
            if (researchCost != 0)
            {
                int scientistCount = (int)(SpaceCenterManagement.Instance?.Researchers ?? 0);
                var researchItem = new BudgetLineItem
                {
                    Name = "Research Teams",
                    Amount = researchCost,
                    Category = BudgetCategory.Personnel,
                    Description = "Scientist salaries for R&D",
                    Icon = "",
                    Count = scientistCount,
                    CountLabel = "scientists"
                };
                if (scientistCount > 0)
                    researchItem.CostPerUnit = researchCost / scientistCount;
                items.Add(researchItem);
            }

            // Astronauts
            var astronautItem = new BudgetLineItem
            {
                Name = "Astronaut Corps",
                Category = BudgetCategory.Personnel,
                Description = "Astronaut salaries and training",
                Icon = ""
            };

            double baseCost = CurrencyUtils.Funds(TransactionReasonsRP0.SalaryCrew,
                -MaintenanceHandler.Instance.NautBaseUpkeepPerDay * periodMultiplier);
            baseCost = Math.Abs(baseCost) * -1; // Ensure negative
            double flightCost = CurrencyUtils.Funds(TransactionReasonsRP0.SalaryCrew,
                -MaintenanceHandler.Instance.NautInFlightUpkeepPerDay * periodMultiplier);
            flightCost = Math.Abs(flightCost) * -1; // Ensure negative
            double trainingCost = CurrencyUtils.Funds(TransactionReasonsRP0.CrewTraining,
                -MaintenanceHandler.Instance.TrainingUpkeepPerDay * periodMultiplier);
            trainingCost = Math.Abs(trainingCost) * -1; // Ensure negative

            astronautItem.SubItems.Add(new BudgetLineItem
            {
                Name = "Base Salary",
                Amount = baseCost,
                Category = BudgetCategory.Personnel,
                Icon = ""
            });

            astronautItem.SubItems.Add(new BudgetLineItem
            {
                Name = "Flight Operations",
                Amount = flightCost,
                Category = BudgetCategory.Personnel,
                Icon = ""
            });

            astronautItem.SubItems.Add(new BudgetLineItem
            {
                Name = "Training",
                Amount = trainingCost,
                Category = BudgetCategory.Personnel,
                Icon = ""
            });

            astronautItem.Amount = baseCost + flightCost + trainingCost;
            
            // Count astronauts
            int nautCount = HighLogic.CurrentGame?.CrewRoster?.GetActiveCrewCount() ?? 0;
            astronautItem.Count = nautCount;
            astronautItem.CountLabel = "astronauts";
            if (nautCount > 0)
                astronautItem.CostPerUnit = astronautItem.Amount / nautCount;
            
            if (astronautItem.Amount != 0)
                items.Add(astronautItem);

            return items;
        }

        /// <summary>
        /// Calculate operational costs (rollout, construction, etc.)
        /// </summary>
        private List<BudgetLineItem> CalculateOperationalCosts(double periodMultiplier)
        {
            var items = new List<BudgetLineItem>();

            if (SpaceCenterManagement.Instance == null)
                return items;

            double periodSeconds = periodMultiplier * 86400d;

            // Rollout/Airlaunch
            double rolloutCost = SpaceCenterManagement.Instance.GetReconRolloutCostOverTime(periodSeconds);
            rolloutCost = Math.Abs(rolloutCost) * -1; // Ensure negative
            if (rolloutCost != 0)
            {
                items.Add(new BudgetLineItem
                {
                    Name = "Rollout/Airlaunch",
                    Amount = rolloutCost,
                    Category = BudgetCategory.Operations,
                    Description = "Vehicle preparation costs",
                    Icon = ""
                });
            }

            // Constructions
            var constructionItem = new BudgetLineItem
            {
                Name = "Construction Projects",
                Category = BudgetCategory.Operations,
                Description = "Active facility construction",
                Icon = ""
            };

            double constructionTotal = 0;
            int constructionCount = 0;
            foreach (var ksc in SpaceCenterManagement.Instance.KSCs)
            {
                if (ksc.Constructions.Count == 0) continue;

                double siteCost = SpaceCenterManagement.Instance.GetConstructionCostOverTime(periodSeconds, ksc);
                siteCost = Math.Abs(siteCost) * -1; // Ensure negative
                constructionTotal += siteCost;

                var siteItem = new BudgetLineItem
                {
                    Name = ksc.KSCName,
                    Amount = siteCost,
                    Category = BudgetCategory.Operations,
                    Icon = ""
                };

                foreach (var construction in ksc.Constructions)
                {
                    double constructionCost = construction.GetConstructionCostOverTime(periodSeconds);
                    constructionCost = Math.Abs(constructionCost) * -1; // Ensure negative
                    constructionCount++;
                    siteItem.SubItems.Add(new BudgetLineItem
                    {
                        Name = construction.GetItemName(),
                        Amount = constructionCost,
                        Category = BudgetCategory.Operations,
                        Icon = ""
                    });
                }

                constructionItem.SubItems.Add(siteItem);
            }

            constructionItem.Amount = constructionTotal;
            constructionItem.Count = constructionCount;
            constructionItem.CountLabel = "constructions";
            if (constructionTotal != 0)
                items.Add(constructionItem);

            return items;
        }

        /// <summary>
        /// Calculate program funding (income from programs)
        /// </summary>
        private List<BudgetLineItem> CalculateProgramCosts(double periodMultiplier)
        {
            var items = new List<BudgetLineItem>();

            if (Programs.ProgramHandler.Instance == null)
                return items;

            double periodSeconds = periodMultiplier * 86400d;
            double currentUT = Planetarium.GetUniversalTime();

            foreach (var program in Programs.ProgramHandler.Instance.ActivePrograms)
            {
                double funding = program.GetFundsForFutureTimestamp(currentUT + periodSeconds) -
                                program.GetFundsForFutureTimestamp(currentUT);
                funding = CurrencyUtils.Funds(TransactionReasonsRP0.ProgramFunding, funding);
                // Programs provide funding (positive income)

                items.Add(new BudgetLineItem
                {
                    Name = program.title,
                    Amount = funding,
                    Category = BudgetCategory.Programs,
                    Description = $"Deadline: {KSPUtil.PrintDate(program.deadlineUT, false)}",
                    Icon = "",
                    Count = 1,
                    CountLabel = "program"
                });
            }

            return items;
        }

        /// <summary>
        /// Calculate income sources
        /// </summary>
        private List<BudgetLineItem> CalculateIncome(double periodMultiplier)
        {
            var items = new List<BudgetLineItem>();

            // Subsidy
            double periodSeconds = periodMultiplier * 86400d;
            double subsidy = MaintenanceHandler.GetAverageSubsidyForPeriod(periodSeconds);
            subsidy = CurrencyUtils.Funds(TransactionReasonsRP0.Subsidy, subsidy) * (periodMultiplier / 365.25d);

            if (subsidy != 0)
            {
                items.Add(new BudgetLineItem
                {
                    Name = "Government Subsidy",
                    Amount = subsidy,
                    Category = BudgetCategory.Income,
                    Description = "Average subsidy income",
                    Icon = ""
                });
            }

            // Unlock Credit (projected)
            if (SpaceCenterManagement.Instance != null)
            {
                double unlockCredit = 0;
                double accumTime = 0;
                for (int i = 0; i < SpaceCenterManagement.Instance.TechList.Count && accumTime < periodSeconds; ++i)
                {
                    var tech = SpaceCenterManagement.Instance.TechList[i];
                    double buildTime = tech.BuildRate > 0d ? tech.TimeLeft : tech.GetTimeLeftEst(accumTime);
                    double timeLeft = periodSeconds - accumTime;
                    if (buildTime > timeLeft)
                        buildTime = timeLeft;
                    if (buildTime <= 0d)
                        continue;
                    unlockCredit += UnlockCreditHandler.Instance.CreditForTime(buildTime);
                    accumTime += buildTime;
                }

                double creditValue = CurrencyUtils.Rate(TransactionReasonsRP0.RateUnlockCreditIncrease) * unlockCredit;
                if (creditValue != 0)
                {
                    items.Add(new BudgetLineItem
                    {
                        Name = "Unlock Credit",
                        Amount = creditValue,
                        Category = BudgetCategory.Income,
                        Description = "Projected research credit earnings",
                        Icon = ""
                    });
                }
            }

            return items;
        }

        /// <summary>
        /// Generate budget alerts based on current financial state
        /// </summary>
        public List<BudgetAlert> GenerateAlerts(BudgetSnapshot snapshot)
        {
            var alerts = new List<BudgetAlert>();

            // Calculate daily cash flow for consistent alerts regardless of period
            double periodMultiplier = GetPeriodMultiplier(snapshot.Period);
            double dailyCashFlow = snapshot.NetCashFlow / periodMultiplier;

            // Low funds warning
            if (snapshot.CurrentFunds < Math.Abs(dailyCashFlow) * 30)
            {
                alerts.Add(new BudgetAlert(
                    $"Low funds! Less than 30 days of runway remaining.",
                    BudgetAlert.AlertLevel.Critical,
                    "Funds"
                ));
            }
            else if (snapshot.CurrentFunds < Math.Abs(dailyCashFlow) * 90)
            {
                alerts.Add(new BudgetAlert(
                    $"Funds running low. Less than 90 days of runway.",
                    BudgetAlert.AlertLevel.Warning,
                    "Funds"
                ));
            }

            // Negative cash flow
            if (dailyCashFlow < 0)
            {
                alerts.Add(new BudgetAlert(
                    $"Negative cash flow: {FormatCurrency(dailyCashFlow)}/day",
                    BudgetAlert.AlertLevel.Warning,
                    "Cash Flow"
                ));
            }

            return alerts;
        }

        /// <summary>
        /// Calculate forecast for reaching a target fund amount
        /// </summary>
        public BudgetForecast CalculateForecast(double targetFunds, BudgetSnapshot snapshot)
        {
            var forecast = new BudgetForecast
            {
                TargetFunds = targetFunds
            };

            if (targetFunds <= snapshot.CurrentFunds)
            {
                forecast.IsAchievable = true;
                forecast.TimeToTarget = 0;
                forecast.DateAtTarget = snapshot.Timestamp;
                forecast.Reason = "Already at target funds";
                return forecast;
            }

            if (snapshot.NetCashFlow <= 0)
            {
                forecast.IsAchievable = false;
                forecast.Reason = "Negative or zero cash flow";
                return forecast;
            }

            double fundsNeeded = targetFunds - snapshot.CurrentFunds;
            double daysNeeded = fundsNeeded / snapshot.NetCashFlow;
            double secondsNeeded = daysNeeded * 86400;

            if (secondsNeeded > FundTargetProject.MaxTime)
            {
                forecast.IsAchievable = false;
                forecast.Reason = $"Would take longer than maximum allowed time";
                return forecast;
            }

            forecast.IsAchievable = true;
            forecast.TimeToTarget = secondsNeeded;
            forecast.DateAtTarget = snapshot.Timestamp + secondsNeeded;

            return forecast;
        }

        /// <summary>
        /// Format currency with appropriate precision
        /// </summary>
        public static string FormatCurrency(double amount, string format = "N0")
        {
            if (amount < 0)
                return $"-{(-amount).ToString(format)}";
            else if (amount > 0)
                return $"+{amount.ToString(format)}";
            else
                return amount.ToString(format);
        }
    }
}
