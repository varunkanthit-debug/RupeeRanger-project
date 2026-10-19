using System;
using System.Collections.Generic;
using UnityEngine;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.Result
{
    [Serializable]
    public class AdviceTip
    {
        public string id;
        public string icon;
        public string category;
        public string title;
        [TextArea(2, 4)]
        public string message;
        public int priority;
        public Color32 accentColor;
    }

    public static class AdviceEngine
    {
        /// <summary>
        /// Analyzes player financial data and returns the top 3 most relevant child-friendly tips.
        /// </summary>
        public static List<AdviceTip> GenerateTopAdvice(int maxTips = 3)
        {
            List<AdviceTip> qualifiedTips = new List<AdviceTip>();

            var gm = GameManager.Instance;
            if (gm == null)
            {
                return GetDefaultFallbackTips();
            }

            int income = Mathf.Max(1000, gm.monthlyIncome);
            int finalSavings = gm.savings;
            int totalExpenses = gm.TotalExpenses + gm.totalEventExpenses + gm.totalInterestPaid;
            int currentDebt = gm.debt;
            int totalLoans = gm.totalLoanTaken;
            int interestPaid = gm.totalInterestPaid;
            int finalScore = Mathf.RoundToInt((finalSavings * 2f) - (currentDebt * 1.5f));

            float entertainmentPercent = (float)gm.entertainmentExpense / income;
            float foodPercent = (float)gm.foodExpense / income;
            float rentPercent = (float)gm.rentExpense / income;
            float transportPercent = (float)gm.transportExpense / income;
            float savingsRatio = (float)finalSavings / income;

            // Rule 1: Emergency Loan Taken (Highest Priority Warning)
            if (totalLoans > 0 || currentDebt > 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_loan_taken",
                    icon = "⚠️",
                    category = "Debt Warning",
                    title = "Beware of Emergency Loans!",
                    message = "You had to borrow emergency money this month. Loans come with extra surcharge fees and interest that eat your future pocket money! Always keep an emergency cash cushion.",
                    priority = 100,
                    accentColor = new Color32(255, 78, 120, 255)
                });
            }

            // Rule 2: Compounding Interest Accrued
            if (interestPaid > 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_interest_paid",
                    icon = "⏳",
                    category = "Interest Snowball",
                    title = "Unpaid Debt Grows Daily!",
                    message = $"You paid ₹{interestPaid:N0} extra in compounding interest! Debt snowballs every single day. Make it your #1 priority to visit the State Bank and repay loans immediately.",
                    priority = 98,
                    accentColor = new Color32(255, 107, 107, 255)
                });
            }

            // Rule 3: Piggy Bank Starving (Savings below 10% of monthly income)
            if (savingsRatio < 0.10f && finalSavings >= 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_low_savings",
                    icon = "🐷",
                    category = "Savings Habit",
                    title = "Feed Your Piggy Bank First!",
                    message = "Your ending savings fell below 10% of your earnings. Remember the golden rule: 'Pay Yourself First'! Put aside 20 rupees out of every 100 as soon as money arrives.",
                    priority = 95,
                    accentColor = new Color32(255, 183, 77, 255)
                });
            }

            // Rule 4: Critical Deficit (Ending in negative savings)
            if (finalSavings < 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_negative_savings",
                    icon = "🚨",
                    category = "Budget Crisis",
                    title = "Stop the Cash Leaks!",
                    message = "You ended the month in a financial deficit! When unexpected bills arrive, cut back immediately on games, parties, and restaurant treats to balance your ledger.",
                    priority = 99,
                    accentColor = new Color32(239, 83, 80, 255)
                });
            }

            // Rule 5: Entertainment & Fun Above 15% of Income
            if (entertainmentPercent > 0.15f)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_entertainment_high",
                    icon = "🎮",
                    category = "Wants vs Needs",
                    title = "Party & Entertainment Alert!",
                    message = $"You spent {(entertainmentPercent * 100f):0}% of your income on entertainment! Having fun is great, but keeping fun expenses under 10-15% protects your safety net.",
                    priority = 91,
                    accentColor = new Color32(186, 104, 200, 255)
                });
            }

            // Rule 6: Multiple Emergencies with Low Cushion
            if (gm.emergencyEventsCount >= 2 && finalSavings < 1500)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_emergency_shield",
                    icon = "🛡️",
                    category = "Emergency Fund",
                    title = "Build an Emergency Shield!",
                    message = "Surprise emergencies like clinic visits and repairs struck you multiple times. Always build an emergency fund equal to 3 months of basic expenses before spending on treats.",
                    priority = 93,
                    accentColor = new Color32(79, 195, 247, 255)
                });
            }

            // Rule 7: High Rent & Lodging Burden (Above 38% of income)
            if (rentPercent > 0.38f)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_rent_heavy",
                    icon = "🏠",
                    category = "Living Costs",
                    title = "Heavy Rent Pressure",
                    message = $"Rent took {(rentPercent * 100f):0}% of your monthly earnings. Sharing a hostel room with a friend or finding affordable student dorms keeps thousands in your pocket.",
                    priority = 81,
                    accentColor = new Color32(255, 138, 101, 255)
                });
            }

            // Rule 8: Dining Out & Cafeteria High (Above 28% of income)
            if (foodPercent > 0.28f)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_food_high",
                    icon = "🍱",
                    category = "Smart Dining",
                    title = "Snack Attack Leaks!",
                    message = "Cafeteria meals and outside snacks took over 28% of your money. Packing homemade lunchboxes and cooking simple meals is both healthy and saves major cash!",
                    priority = 78,
                    accentColor = new Color32(255, 213, 79, 255)
                });
            }

            // Rule 9: High Commute & Transit Expense (Above 15% of income)
            if (transportPercent > 0.15f)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_transport_high",
                    icon = "🚲",
                    category = "Green Commute",
                    title = "Cycle & Bus to Save More!",
                    message = "Auto-rickshaws and cabs added up quickly. Using a student bus pass or riding a bicycle around town gives you great exercise and saves you hundreds of rupees!",
                    priority = 74,
                    accentColor = new Color32(129, 199, 132, 255)
                });
            }

            // Rule 10: Impulse Event Spending (Spent on expensive optional events)
            if (gm.impulseSpendingEventsCount >= 2)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_impulse_spending",
                    icon = "💸",
                    category = "Impulse Control",
                    title = "Say 'No' to Peer Pressure!",
                    message = "You spent on costly birthday parties or treats when friends asked. It's totally okay to say 'not this time' or find cheaper ways to celebrate together!",
                    priority = 88,
                    accentColor = new Color32(240, 98, 146, 255)
                });
            }

            // Rule 11: Responsible Early Loan Repayer
            if (gm.playerRepaidLoanEarly)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_early_repayment",
                    icon = "🏦",
                    category = "Credit Hero",
                    title = "Outstanding Debt Repayment!",
                    message = "You visited the State Bank and paid off your debt early! That prevented interest from compounding and earned you valuable credit rating points. True financial mastery!",
                    priority = 89,
                    accentColor = new Color32(0, 230, 118, 255)
                });
            }

            // Rule 12: Prudent Event Decision Maker
            if (gm.wiseEventChoicesCount >= 2)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_wise_choices",
                    icon = "💡",
                    category = "Smart Decisions",
                    title = "Wise Trade-Offs Master!",
                    message = "When sudden dilemmas arrived, you made smart trade-offs and saved money instead of splurging. Thinking before swiping is the superpower of wealth!",
                    priority = 85,
                    accentColor = new Color32(100, 255, 218, 255)
                });
            }

            // Rule 13: Side-Hustle & Windfall Reinvestment
            if (gm.windfallEventsCount >= 1 && finalSavings > gm.initialSavings)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_windfall_saved",
                    icon = "💼",
                    category = "Income Growth",
                    title = "Windfall Multiplier!",
                    message = "When you earned bonus cash from tutoring or freelancing, you banked it into savings rather than spending it immediately. That's how champions build wealth!",
                    priority = 86,
                    accentColor = new Color32(0, 229, 255, 255)
                });
            }

            // Rule 14: Super Saver Champion (Savings >= 30% of income with 0 loans)
            if (savingsRatio >= 0.30f && totalLoans == 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_super_saver",
                    icon = "🌟",
                    category = "Wealth Builder",
                    title = "Super Saver Champion!",
                    message = $"Incredible job! You saved {(savingsRatio * 100f):0}% of your total earnings without borrowing a single rupee. Your piggy bank is turning into a mighty treasure chest!",
                    priority = 96,
                    accentColor = new Color32(0, 240, 160, 255)
                });
            }

            // Rule 15: Golden 70/20/10 Budget Balance
            if (totalExpenses <= income * 0.75f && savingsRatio >= 0.20f && totalLoans == 0)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_budget_master",
                    icon = "⚖️",
                    category = "Financial Balance",
                    title = "The Golden Budget Balance!",
                    message = "You balanced living costs, savings, and fun with textbook perfection. Keeping needs under 75% gave you total freedom and financial peace of mind.",
                    priority = 90,
                    accentColor = new Color32(105, 240, 174, 255)
                });
            }

            // Rule 16: Legendary Score Achievement
            if (finalScore >= 15000)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_legendary_ranger",
                    icon = "👑",
                    category = "Hall of Fame",
                    title = "Legendary Rupee Ranger!",
                    message = "Phenomenal run! You navigated monthly fevers, college fees, and daily town tasks with flying colors. You have graduated to a true master of personal finance!",
                    priority = 97,
                    accentColor = new Color32(255, 215, 0, 255)
                });
            }

            // Rule 17: Resilient Fighter (Surviving tough month)
            if (currentDebt > 0 && finalScore < 5000)
            {
                qualifiedTips.Add(new AdviceTip
                {
                    id = "rule_resilience",
                    icon = "🌱",
                    category = "Resilience",
                    title = "Every Mistake is a Stepping Stone!",
                    message = "This month was stormy and tough, but you fought through all 30 days. Analyze where your budget leaked, budget a bit tighter, and you'll bounce back even stronger next time!",
                    priority = 94,
                    accentColor = new Color32(144, 202, 249, 255)
                });
            }

            // Sort by priority descending
            qualifiedTips.Sort((a, b) => b.priority.CompareTo(a.priority));

            // If fewer than maxTips qualify, supplement from fallback tips
            if (qualifiedTips.Count < maxTips)
            {
                var fallbacks = GetDefaultFallbackTips();
                foreach (var fb in fallbacks)
                {
                    if (!qualifiedTips.Exists(t => t.id == fb.id))
                    {
                        qualifiedTips.Add(fb);
                        if (qualifiedTips.Count >= maxTips) break;
                    }
                }
            }

            return qualifiedTips.GetRange(0, Mathf.Min(maxTips, qualifiedTips.Count));
        }

        private static List<AdviceTip> GetDefaultFallbackTips()
        {
            return new List<AdviceTip>
            {
                new AdviceTip
                {
                    id = "fb_pay_yourself_first",
                    icon = "🐷",
                    category = "Core Habit",
                    title = "Pay Yourself First!",
                    message = "Whenever you earn or receive pocket money, tuck 20% into savings before spending on snacks or toys. This makes saving effortless!",
                    priority = 50,
                    accentColor = new Color32(0, 240, 160, 255)
                },
                new AdviceTip
                {
                    id = "fb_emergency_cushion",
                    icon = "🛡️",
                    category = "Safety Net",
                    title = "Always Keep an Umbrella!",
                    message = "Surprise expenses like broken phone screens or clinic visits happen to everyone. A small cash buffer prevents needing stressful loans.",
                    priority = 45,
                    accentColor = new Color32(79, 195, 247, 255)
                },
                new AdviceTip
                {
                    id = "fb_needs_vs_wants",
                    icon = "🎯",
                    category = "Smart Shopping",
                    title = "Needs Before Wants",
                    message = "Needs are things you must have (like books and food), while wants are treats (like extra toys). Cover all needs first to stay safe!",
                    priority = 40,
                    accentColor = new Color32(255, 215, 0, 255)
                }
            };
        }
    }
}
