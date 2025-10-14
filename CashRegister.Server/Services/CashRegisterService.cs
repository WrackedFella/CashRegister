namespace CashRegister.Server.Services
{
    public class CashRegisterService
    {
        private static readonly Dictionary<string, decimal> Denominations = new()
        {
            { "dollar", 1.00m },
            { "quarter", 0.25m },
            { "dime", 0.10m },
            { "nickel", 0.05m },
            { "penny", 0.01m }
        };

        public string[] CalculateChangeForTransactions(string[] transactions)
        {
            var results = new List<string>();
            
            foreach (var transaction in transactions)
            {
                var parts = transaction.Split(',');
                if (parts.Length == 2 && 
                    decimal.TryParse(parts[0], out decimal amountOwed) && 
                    decimal.TryParse(parts[1], out decimal amountPaid))
                {
                    var changeAmount = amountPaid - amountOwed;
                    
                    // Only calculate change for amounts not divisible by 3
                    if (amountOwed % 3 != 0)
                    {
                        var changeDescription = CalculateChange(changeAmount);
                        results.Add(changeDescription);
                    }
                }
            }
            
            return results.ToArray();
        }

        private string CalculateChange(decimal changeAmount)
        {
            if (changeAmount <= 0)
                return "";

            var changeParts = new List<string>();
            var remainingAmount = Math.Round(changeAmount, 2);

            foreach (var denomination in Denominations)
            {
                var count = (int)(remainingAmount / denomination.Value);
                if (count > 0)
                {
                    var denominationName = GetPluralDenomination(denomination.Key, count);
                    changeParts.Add($"{count} {denominationName}");
                    remainingAmount = Math.Round(remainingAmount - (count * denomination.Value), 2);
                }
            }

            return string.Join(",", changeParts);
        }

        private string GetPluralDenomination(string denominationKey, int count)
        {
            if (count == 1)
                return denominationKey;

            return denominationKey switch
            {
                "penny" => "pennies",
                _ => denominationKey + "s"
            };
        }
    }
}