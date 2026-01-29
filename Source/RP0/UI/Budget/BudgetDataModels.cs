using System;
using System.Collections.Generic;
using System.Linq;

namespace RP0.UI.Budget
{
    /// <summary>
    /// Represents a time period for budget calculations
    /// </summary>
    public enum BudgetPeriod
    {
        Day,
        Month,
        Year
    }

    /// <summary>
    /// Category types for budget items
    /// </summary>
    public enum BudgetCategory
    {
        Facilities,
        Personnel,
        Operations,
        Programs,
        Income,
        Other
    }

    /// <summary>
    /// Represents a single line item in the budget
    /// </summary>
    public class BudgetLineItem
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public BudgetCategory Category { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public List<BudgetLineItem> SubItems { get; set; }
        public bool IsExpanded { get; set; }
        public Action OnClick { get; set; }
        
        // Metadata for displaying counts
        public int Count { get; set; }
        public string CountLabel { get; set; }
        public double CostPerUnit { get; set; }

        public BudgetLineItem()
        {
            SubItems = new List<BudgetLineItem>();
            IsExpanded = false;
            Count = 0;
            CountLabel = null;
            CostPerUnit = 0;
        }

        public bool HasSubItems => SubItems != null && SubItems.Count > 0;

        public double GetTotal()
        {
            double total = Amount;
            if (HasSubItems)
            {
                foreach (var item in SubItems)
                {
                    total += item.GetTotal();
                }
            }
            return total;
        }
    }

    /// <summary>
    /// Represents a complete budget snapshot
    /// </summary>
    public class BudgetSnapshot
    {
        public double Timestamp { get; set; }
        public BudgetPeriod Period { get; set; }
        public List<BudgetLineItem> Expenses { get; set; }
        public List<BudgetLineItem> Income { get; set; }
        public double CurrentFunds { get; set; }
        public double ProjectedFunds { get; set; }

        public BudgetSnapshot()
        {
            Expenses = new List<BudgetLineItem>();
            Income = new List<BudgetLineItem>();
            Timestamp = Planetarium.GetUniversalTime();
        }

        public double TotalExpenses
        {
            get
            {
                double total = 0;
                foreach (var item in Expenses)
                {
                    total += item.GetTotal();
                }
                return total;
            }
        }

        public double TotalIncome
        {
            get
            {
                double total = 0;
                foreach (var item in Income)
                {
                    total += item.GetTotal();
                }
                return total;
            }
        }

        public double NetCashFlow => TotalIncome + TotalExpenses; // Expenses are negative
    }

    /// <summary>
    /// Budget alert/warning system
    /// </summary>
    public class BudgetAlert
    {
        public enum AlertLevel
        {
            Info,
            Warning,
            Critical
        }

        public string Message { get; set; }
        public AlertLevel Level { get; set; }
        public string Category { get; set; }
        public Action OnClick { get; set; }

        public BudgetAlert(string message, AlertLevel level, string category = null)
        {
            Message = message;
            Level = level;
            Category = category;
        }
    }

    /// <summary>
    /// Represents a budget forecast/projection
    /// </summary>
    public class BudgetForecast
    {
        public double TargetFunds { get; set; }
        public double TimeToTarget { get; set; }
        public double DateAtTarget { get; set; }
        public bool IsAchievable { get; set; }
        public string Reason { get; set; }
    }

}
