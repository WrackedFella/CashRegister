using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashRegister.Server.Services
{
    public class CashRegisterService
    {
        private readonly CashRegisterConfiguration _configuration;
        private readonly Random _random;
        private readonly ILogger<CashRegisterService> _logger;

        public CashRegisterService(CashRegisterConfiguration configuration, Random? random = null, ILogger<CashRegisterService>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _random = random ?? new Random();
            _logger = logger ?? NullLogger<CashRegisterService>.Instance;
        }

        public string[] CalculateChangeForTransactions(string[] transactions)
        {
            _logger.LogInformation("Processing {TransactionCount} transactions", transactions.Length);
            
            var results = new List<string>();
            
            foreach (var transaction in transactions)
            {
                try
                {
                    _logger.LogDebug("Processing transaction: {Transaction}", transaction);
                    
                    var parts = transaction.Split(',');
                    if (parts.Length == 2 && 
                        decimal.TryParse(parts[0], out decimal amountOwed) && 
                        decimal.TryParse(parts[1], out decimal amountPaid))
                    {
                        var changeAmount = amountPaid - amountOwed;
                        
                        if (changeAmount < 0)
                        {
                            _logger.LogWarning("Invalid transaction: amount paid ({AmountPaid}) is less than amount owed ({AmountOwed})", amountPaid, amountOwed);
                            continue;
                        }
                        
                        string changeDescription;
                        if (amountOwed % 3 == 0)
                        {
                            _logger.LogDebug("Using random change calculation for amount owed {AmountOwed} (divisible by 3)", amountOwed);
                            // Random denomination selection for amounts divisible by 3
                            changeDescription = CalculateRandomChange(changeAmount);
                        }
                        else
                        {
                            _logger.LogDebug("Using standard change calculation for amount owed {AmountOwed}", amountOwed);
                            // Standard highest-to-lowest denomination for amounts not divisible by 3
                            changeDescription = CalculateStandardChange(changeAmount);
                        }
                        
                        if (!string.IsNullOrEmpty(changeDescription))
                        {
                            _logger.LogDebug("Calculated change: {ChangeDescription}", changeDescription);
                            results.Add(changeDescription);
                        }
                        else
                        {
                            _logger.LogWarning("No change calculated for transaction: {Transaction}", transaction);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid transaction format: {Transaction}. Expected format: 'amountOwed,amountPaid'", transaction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing transaction: {Transaction}", transaction);
                    throw;
                }
            }
            
            _logger.LogInformation("Successfully processed {ProcessedCount} out of {TotalCount} transactions", results.Count, transactions.Length);
            return results.ToArray();
        }

        private string CalculateStandardChange(decimal changeAmount)
        {
            if (changeAmount <= 0)
                return "";

            var changeParts = new List<string>();
            var remainingAmount = Math.Round(changeAmount, 2);

            foreach (var denomination in _configuration.CurrencyDenominations)
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
            var denominationsList = _configuration.CurrencyDenominations.ToList();

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
            foreach (var denominationKey in _configuration.CurrencyDenominations.Keys)
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
            var denominationsList = _configuration.CurrencyDenominations.Keys.ToList();

            // Randomly shuffle denomination order for each generation
            var shuffledDenominations = denominationsList.OrderBy(x => _random.Next()).ToList();

            foreach (var denominationKey in shuffledDenominations)
            {
                var denominationValue = _configuration.CurrencyDenominations[denominationKey];
                
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
                    if (randomFactor < _configuration.RandomProbabilities.SkipDenominationProbability) // Configurable chance to skip this denomination entirely
                    {
                        useCount = 0;
                    }
                    else if (randomFactor < _configuration.RandomProbabilities.SkipDenominationProbability + _configuration.RandomProbabilities.UsePartialAmountProbability) // Configurable chance to use partial amount
                    {
                        var minCount = 1;
                        var maxCount = Math.Max(1, maxPossible / 2 + 1);
                        useCount = maxCount > minCount ? _random.Next(minCount, maxCount) : minCount;
                    }
                    else // Configurable chance to use more (but not necessarily all)
                    {
                        var minCount = Math.Max(1, maxPossible / 2);
                        var maxCount = Math.Min(maxPossible + 1, _configuration.RandomProbabilities.MaxCoinsPerDenomination + 1);
                        useCount = maxCount > minCount ? _random.Next(minCount, maxCount) : minCount;
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

            foreach (var denomination in _configuration.CurrencyDenominations)
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