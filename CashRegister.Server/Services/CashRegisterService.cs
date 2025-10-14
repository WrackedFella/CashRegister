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

        private readonly Random _random;

        public CashRegisterService(Random? random = null)
        {
            _random = random ?? new Random();
        }

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
                    
                    string changeDescription;
                    if (amountOwed % 3 == 0)
                    {
                        // Random denomination selection for amounts divisible by 3
                        changeDescription = CalculateRandomChange(changeAmount);
                    }
                    else
                    {
                        // Standard highest-to-lowest denomination for amounts not divisible by 3
                        changeDescription = CalculateStandardChange(changeAmount);
                    }
                    
                    if (!string.IsNullOrEmpty(changeDescription))
                    {
                        results.Add(changeDescription);
                    }
                }
            }
            
            return results.ToArray();
        }

        private string CalculateStandardChange(decimal changeAmount)
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

        private string CalculateRandomChange(decimal changeAmount)
        {
            if (changeAmount <= 0)
                return "";

            var remainingAmount = Math.Round(changeAmount, 2);
            var changeCombination = new Dictionary<string, int>();
            var denominationsList = Denominations.ToList();

            // Generate multiple random valid combinations and pick one
            var validCombinations = new List<Dictionary<string, int>>();
            
            // Try to generate several different valid combinations
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var combination = GenerateRandomValidCombination(remainingAmount);
                if (combination != null && !CombinationExists(validCombinations, combination))
                {
                    validCombinations.Add(combination);
                }
                
                if (validCombinations.Count >= 5) break; // Stop when we have enough options
            }

            // If we couldn't generate alternatives, create some manually different combinations
            if (validCombinations.Count == 0)
            {
                validCombinations.Add(GenerateStandardCombination(remainingAmount));
            }

            // Pick a random combination from our valid options
            var chosenCombination = validCombinations[_random.Next(validCombinations.Count)];

            // Convert to result format
            var changeParts = new List<string>();
            foreach (var denominationKey in Denominations.Keys)
            {
                if (chosenCombination.ContainsKey(denominationKey) && chosenCombination[denominationKey] > 0)
                {
                    var count = chosenCombination[denominationKey];
                    var denominationName = GetPluralDenomination(denominationKey, count);
                    changeParts.Add($"{count} {denominationName}");
                }
            }

            return string.Join(",", changeParts);
        }

        private Dictionary<string, int>? GenerateRandomValidCombination(decimal amount)
        {
            var combination = new Dictionary<string, int>();
            var remaining = amount;
            var denominationsList = Denominations.Keys.ToList();

            // Randomly shuffle denomination order for each generation
            var shuffledDenominations = denominationsList.OrderBy(x => _random.Next()).ToList();

            foreach (var denominationKey in shuffledDenominations)
            {
                var denominationValue = Denominations[denominationKey];
                
                if (remaining >= denominationValue)
                {
                    var maxPossible = (int)(remaining / denominationValue);
                    
                    // Introduce more randomness - sometimes use fewer coins, sometimes more
                    int useCount;
                    if (denominationKey == "penny")
                    {
                        // For pennies, use all remaining to ensure exact change
                        useCount = (int)Math.Round(remaining / denominationValue);
                    }
                    else
                    {
                        // For other denominations, randomly choose between 0 and max possible
                        // Bias towards using some coins but not always the maximum
                        var randomFactor = _random.NextDouble();
                        if (randomFactor < 0.3) // 30% chance to skip this denomination entirely
                        {
                            useCount = 0;
                        }
                        else if (randomFactor < 0.7) // 40% chance to use partial amount
                        {
                            useCount = _random.Next(1, Math.Max(1, maxPossible / 2 + 1));
                        }
                        else // 30% chance to use more (but not necessarily all)
                        {
                            useCount = _random.Next(Math.Max(1, maxPossible / 2), maxPossible + 1);
                        }
                    }

                    if (useCount > 0)
                    {
                        combination[denominationKey] = useCount;
                        remaining = Math.Round(remaining - (useCount * denominationValue), 2);
                    }
                }
            }

            // If we have remaining amount, convert to pennies
            if (remaining > 0.001m)
            {
                var remainingPennies = (int)Math.Round(remaining / 0.01m);
                if (combination.ContainsKey("penny"))
                {
                    combination["penny"] += remainingPennies;
                }
                else
                {
                    combination["penny"] = remainingPennies;
                }
            }

            return combination;
        }

        private Dictionary<string, int> GenerateStandardCombination(decimal amount)
        {
            var combination = new Dictionary<string, int>();
            var remaining = amount;

            foreach (var denomination in Denominations)
            {
                var count = (int)(remaining / denomination.Value);
                if (count > 0)
                {
                    combination[denomination.Key] = count;
                    remaining = Math.Round(remaining - (count * denomination.Value), 2);
                }
            }

            return combination;
        }

        private bool CombinationExists(List<Dictionary<string, int>> combinations, Dictionary<string, int> newCombination)
        {
            return combinations.Any(existing => 
                existing.Count == newCombination.Count &&
                existing.All(kvp => newCombination.ContainsKey(kvp.Key) && newCombination[kvp.Key] == kvp.Value));
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