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
                    var changeDescription = ProcessTransaction(transaction);
                    if (!string.IsNullOrEmpty(changeDescription))
                    {
                        results.Add(changeDescription);
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

        private string ProcessTransaction(string transaction)
        {
            _logger.LogDebug("Processing transaction: {Transaction}", transaction);
            
            var parts = transaction.Split(',');
            if (parts.Length != 2 || 
                !decimal.TryParse(parts[0], out decimal amountOwed) || 
                !decimal.TryParse(parts[1], out decimal amountPaid))
            {
                _logger.LogWarning("Invalid transaction format: {Transaction}. Expected format: 'amountOwed,amountPaid'", transaction);
                return string.Empty;
            }

            var changeAmount = amountPaid - amountOwed;
            
            if (changeAmount < 0)
            {
                _logger.LogWarning("Invalid transaction: amount paid ({AmountPaid}) is less than amount owed ({AmountOwed})", amountPaid, amountOwed);
                return string.Empty;
            }

            // Convert to cents to check divisibility by 3 (avoids floating point issues)
            var amountOwedInCents = (int)Math.Round(amountOwed * 100);
            var changeDescription = amountOwedInCents % 3 == 0 
                ? CalculateRandomChange(changeAmount) 
                : CalculateStandardChange(changeAmount);

            _logger.LogDebug("Calculated change: {ChangeDescription}", changeDescription);
            return changeDescription;
        }

        private string CalculateStandardChange(decimal changeAmount)
        {
            if (changeAmount <= 0) return string.Empty;

            var combination = GenerateStandardCombination(changeAmount);
            return FormatChangeCombination(combination);
        }

        private string CalculateRandomChange(decimal changeAmount)
        {
            if (changeAmount <= 0) return string.Empty;

            var validCombinations = GenerateRandomCombinations(changeAmount);
            var chosenCombination = validCombinations[_random.Next(validCombinations.Count)];
            
            return FormatChangeCombination(chosenCombination);
        }

        private List<Dictionary<string, int>> GenerateRandomCombinations(decimal amount)
        {
            var validCombinations = new List<Dictionary<string, int>>();
            
            for (int attempt = 0; attempt < 20 && validCombinations.Count < 5; attempt++)
            {
                var combination = GenerateRandomValidCombination(amount);
                if (combination != null && !CombinationExists(validCombinations, combination))
                {
                    validCombinations.Add(combination);
                }
            }

            if (validCombinations.Count == 0)
            {
                validCombinations.Add(GenerateStandardCombination(amount));
            }

            return validCombinations;
        }

        private string FormatChangeCombination(Dictionary<string, int> combination)
        {
            var changeParts = _configuration.CurrencyDenominations.Keys
                .Where(key => combination.ContainsKey(key) && combination[key] > 0)
                .Select(key => $"{combination[key]} {GetPluralDenomination(key, combination[key])}");
            
            return string.Join(",", changeParts);
        }

        private Dictionary<string, int>? GenerateRandomValidCombination(decimal amount)
        {
            var combination = new Dictionary<string, int>();
            var remaining = amount;
            var shuffledDenominations = _configuration.CurrencyDenominations.Keys
                .OrderBy(x => _random.Next())
                .ToList();

            foreach (var denominationKey in shuffledDenominations)
            {
                var denominationValue = _configuration.CurrencyDenominations[denominationKey];
                
                if (remaining >= denominationValue)
                {
                    var useCount = CalculateRandomCoinCount(denominationKey, remaining, denominationValue);

                    if (useCount > 0)
                    {
                        combination[denominationKey] = useCount;
                        remaining = Math.Round(remaining - (useCount * denominationValue), 2);
                    }
                }
            }

            AddRemainingPennies(combination, remaining);
            return combination;
        }

        private int CalculateRandomCoinCount(string denominationKey, decimal remaining, decimal denominationValue)
        {
            var maxPossible = (int)(remaining / denominationValue);
            
            if (denominationKey == "penny")
            {
                return (int)Math.Round(remaining / denominationValue);
            }

            var randomFactor = _random.NextDouble();
            var probabilities = _configuration.RandomProbabilities;

            if (randomFactor < probabilities.SkipDenominationProbability)
            {
                return 0;
            }
            
            if (randomFactor < probabilities.SkipDenominationProbability + probabilities.UsePartialAmountProbability)
            {
                var maxCount = Math.Max(1, maxPossible / 2 + 1);
                return maxCount > 1 ? _random.Next(1, maxCount) : 1;
            }
            
            var minCount = Math.Max(1, maxPossible / 2);
            var upperLimit = Math.Min(maxPossible + 1, probabilities.MaxCoinsPerDenomination + 1);
            return upperLimit > minCount ? _random.Next(minCount, upperLimit) : minCount;
        }

        private static void AddRemainingPennies(Dictionary<string, int> combination, decimal remaining)
        {
            if (remaining > 0.001m)
            {
                var remainingPennies = (int)Math.Round(remaining / 0.01m);
                combination["penny"] = combination.GetValueOrDefault("penny") + remainingPennies;
            }
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

        private static bool CombinationExists(List<Dictionary<string, int>> combinations, Dictionary<string, int> newCombination) =>
            combinations.Any(existing => 
                existing.Count == newCombination.Count &&
                existing.All(kvp => newCombination.TryGetValue(kvp.Key, out var value) && value == kvp.Value));

        private static string GetPluralDenomination(string denominationKey, int count) =>
            count == 1 ? denominationKey : denominationKey switch
            {
                "penny" => "pennies",
                _ => denominationKey + "s"
            };
    }
}