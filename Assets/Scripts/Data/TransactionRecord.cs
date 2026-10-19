using System;

namespace RupeeRanger.Data
{
    [Serializable]
    public class TransactionRecord
    {
        public int day;
        public string description;
        public int amount; // positive for income, negative for expense
        public string category;

        public TransactionRecord(int day, string description, int amount, string category)
        {
            this.day = day;
            this.description = description;
            this.amount = amount;
            this.category = category;
        }
    }
}
