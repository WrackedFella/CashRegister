using CashRegister.Server.Services;

namespace CashRegister.UnitTests
{
    public class CashRegisterTests
    {
        private static readonly CashRegisterConfiguration config = new()
        {
            CurrencyDenominations = new Dictionary<string, decimal>
            {
                { "dollar", 1.00m },
                { "quarter", 0.25m },
                { "dime", 0.10m },
                { "nickel", 0.05m },
                { "penny", 0.01m }
            },
            CurrencySymbol = "$",
            RandomProbabilities = new RandomChangeProbabilities
            {
                SkipDenominationProbability = 0.3,
                UsePartialAmountProbability = 0.4,
                UseFullAmountProbability = 0.3,
                MaxCoinsPerDenomination = 10
            }
        };

        [Fact]
        public void CalculateChangeForTransactions_WhenAmountOwedNotDivisibleByThree_ReturnsStandardChangeDescriptions()
        {
            // Arrange
            var service = new CashRegisterService(config);
            var transactions = new[] { "2.12,3.00", "1.97,2.00" };
            var expected = new[] { "3 quarters,1 dime,3 pennies", "3 pennies" };

            // Act
            var actual = service.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CalculateChangeForTransactions_WhenAmountOwedDivisibleByThree_ReturnsRandomChangeDescriptions()
        {
            // Arrange
            var service = new CashRegisterService(config, new Random(42));
            var transactions = new[] { "3.00,5.00", "6.00,10.00" };

            // Act
            var results = service.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, result => Assert.NotEmpty(result));
            Assert.True(VerifyChangeAmount(results[0], 2.00m), $"First result should equal $2.00, got: {results[0]}");
            Assert.True(VerifyChangeAmount(results[1], 4.00m), $"Second result should equal $4.00, got: {results[1]}");
        }

        [Fact]
        public void CalculateChangeForTransactions_WithMixedDivisibilityByThree_ReturnsBothStandardAndRandomResults()
        {
            // Arrange
            var service = new CashRegisterService(config, new Random(123));
            var transactions = new[] { "2.12,3.00", "3.33,5.00", "1.97,2.00" };

            // Act
            var results = service.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(3, results.Length);
            Assert.Equal("3 quarters,1 dime,3 pennies", results[0]);
            Assert.Equal("3 pennies", results[2]);
            Assert.True(VerifyChangeAmount(results[1], 1.67m), $"Second result should equal $1.67, got: {results[1]}");
        }

        [Fact]
        public void CalculateChangeForTransactions_SpecificExampleFromRequirement_ReturnsCorrectResults()
        {
            // Arrange
            var service = new CashRegisterService(config);
            var transactions = new[] { "2.12,3.00", "1.97,2.00", "3.33,5.00" };

            // Act
            var results = service.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(3, results.Length);
            Assert.Equal("3 quarters,1 dime,3 pennies", results[0]);
            Assert.Equal("3 pennies", results[1]);
            Assert.True(VerifyChangeAmount(results[2], 1.67m), $"Third result should equal $1.67, got: {results[2]}");
        }

        [Fact]
        public void CalculateRandomChange_MultipleRuns_CanProduceDifferentResults()
        {
            // Arrange
            var transactions = new[] { "3.33,5.00" };
            var results = new HashSet<string>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                var service = new CashRegisterService(config);
                var result = service.CalculateChangeForTransactions(transactions);
                if (result.Length > 0) results.Add(result[0]);
            }

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, result => Assert.True(VerifyChangeAmount(result, 1.67m), $"Result should equal $1.67, got: {result}"));
        }

        [Fact]
        public void CalculateChangeForTransactions_WithInvalidTransaction_LogsWarning()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService(config);
            var transactions = new[]
            {
                "2.12,3.00", // Valid transaction
                "invalid,format", // Invalid transaction
                "3.33,5.00"  // Valid transaction
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(2, actualResults.Length); // Only valid transactions should produce results
            Assert.Equal("3 quarters,1 dime,3 pennies", actualResults[0]);
            Assert.True(VerifyChangeAmount(actualResults[1], 1.67m), $"Third result should equal $1.67, got: {actualResults[1]}");
        }

        [Fact]
        public void CalculateChangeForTransactions_WithInsufficientPayment_LogsWarning()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService(config);
            var transactions = new[]
            {
                "5.00,3.00" // Amount paid is less than amount owed
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Empty(actualResults); // No change should be calculated
        }
        
        private bool VerifyChangeAmount(string changeDescription, decimal expectedAmount)
        {
            if (string.IsNullOrEmpty(changeDescription))
                return expectedAmount == 0;

            var totalValue = changeDescription.Split(',')
                .Select(part => part.Trim())
                .Where(part => part.IndexOf(' ') > 0)
                .Sum(part => CalculatePartValue(part));

            return Math.Abs(totalValue - expectedAmount) < 0.001m;
        }

        private decimal CalculatePartValue(string part)
        {
            var spaceIndex = part.IndexOf(' ');
            if (!int.TryParse(part[..spaceIndex], out int count))
                return 0;

            var denomination = part[(spaceIndex + 1)..].Trim();
            var denominationEntry = config.CurrencyDenominations.FirstOrDefault(d =>
                d.Key == denomination || GetPluralDenomination(d.Key, count) == denomination);

            return denominationEntry.Equals(default(KeyValuePair<string, decimal>)) 
                ? 0 
                : count * denominationEntry.Value;
        }

        private static string GetPluralDenomination(string denominationKey, int count) =>
            count == 1 ? denominationKey : denominationKey switch
            {
                "penny" => "pennies",
                _ => denominationKey + "s"
            };
    }
}
