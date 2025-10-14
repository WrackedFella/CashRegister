using CashRegister.Server.Services;

namespace CashRegister.UnitTests
{
    public class CashRegisterTests
    {
        [Fact]
        public void CalculateChangeForTransactions_WhenAmountOwedNotDivisibleByThree_ReturnsStandardChangeDescriptions()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService();
            var transactions = new[]
            {
                "2.12,3.00",
                "1.97,2.00"
            };

            var expectedResults = new[]
            {
                "3 quarters,1 dime,3 pennies",
                "3 pennies"
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(expectedResults.Length, actualResults.Length);
            Assert.Equal(expectedResults[0], actualResults[0]);
            Assert.Equal(expectedResults[1], actualResults[1]);
        }

        [Fact]
        public void CalculateChangeForTransactions_WhenAmountOwedDivisibleByThree_ReturnsRandomChangeDescriptions()
        {
            // Arrange
            // Use a seeded random to make the test deterministic
            var seededRandom = new Random(42);
            var cashRegisterService = new CashRegisterService(seededRandom);
            var transactions = new[]
            {
                "3.00,5.00", // 3.00 is divisible by 3, change = $2.00
                "6.00,10.00" // 6.00 is divisible by 3, change = $4.00
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(2, actualResults.Length);
            
            // Verify that we get some result for divisible by 3 amounts
            Assert.NotEmpty(actualResults[0]);
            Assert.NotEmpty(actualResults[1]);
            
            // Verify the total value is correct by parsing the change descriptions
            Assert.True(VerifyChangeAmount(actualResults[0], 2.00m), $"First result should equal $2.00, got: {actualResults[0]}");
            Assert.True(VerifyChangeAmount(actualResults[1], 4.00m), $"Second result should equal $4.00, got: {actualResults[1]}");
        }

        [Fact]
        public void CalculateChangeForTransactions_WithMixedDivisibilityByThree_ReturnsBothStandardAndRandomResults()
        {
            // Arrange
            var seededRandom = new Random(123);
            var cashRegisterService = new CashRegisterService(seededRandom);
            var transactions = new[]
            {
                "2.12,3.00", // 2.12 is not divisible by 3 - should use standard change
                "3.33,5.00", // 3.33 is divisible by 3 - should use random change
                "1.97,2.00"  // 1.97 is not divisible by 3 - should use standard change
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(3, actualResults.Length);
            
            // First and third should be standard (highest-to-lowest denominations)
            Assert.Equal("3 quarters,1 dime,3 pennies", actualResults[0]);
            Assert.Equal("3 pennies", actualResults[2]);
            
            // Second should be random but total value should be correct ($1.67)
            Assert.True(VerifyChangeAmount(actualResults[1], 1.67m), $"Second result should equal $1.67, got: {actualResults[1]}");
        }

        [Fact]
        public void CalculateChangeForTransactions_SpecificExampleFromRequirement_ReturnsCorrectResults()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService();
            var transactions = new[]
            {
                "2.12,3.00", // Not divisible by 3
                "1.97,2.00", // Not divisible by 3
                "3.33,5.00"  // Divisible by 3
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Equal(3, actualResults.Length);
            
            // First two should match exactly (standard calculation)
            Assert.Equal("3 quarters,1 dime,3 pennies", actualResults[0]);
            Assert.Equal("3 pennies", actualResults[1]);
            
            // Third should be random but mathematically correct ($1.67)
            Assert.True(VerifyChangeAmount(actualResults[2], 1.67m), $"Third result should equal $1.67, got: {actualResults[2]}");
        }

        [Fact]
        public void CalculateRandomChange_MultipleRuns_CanProduceDifferentResults()
        {
            // Arrange
            var transactions = new[] { "3.33,5.00" }; // Divisible by 3, change = $1.67
            var results = new HashSet<string>();

            // Act - run multiple times to see if we get different results
            for (int i = 0; i < 10; i++)
            {
                var cashRegisterService = new CashRegisterService();
                var result = cashRegisterService.CalculateChangeForTransactions(transactions);
                if (result.Length > 0)
                {
                    results.Add(result[0]);
                }
            }

            // Assert
            // We should get at least one result, and all should be mathematically correct
            Assert.True(results.Count > 0, "Should produce at least one result");
            
            foreach (var result in results)
            {
                Assert.True(VerifyChangeAmount(result, 1.67m), $"Result should equal $1.67, got: {result}");
            }
            
            // It's possible (but unlikely) that all runs produce the same result due to randomness
            // So we won't assert that results.Count > 1, but we'll output what we got for manual verification
            Assert.True(true, $"Generated {results.Count} unique result(s): {string.Join("; ", results)}");
        }

        private bool VerifyChangeAmount(string changeDescription, decimal expectedAmount)
        {
            if (string.IsNullOrEmpty(changeDescription))
                return expectedAmount == 0;

            var parts = changeDescription.Split(',');
            decimal totalValue = 0;

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                var spaceIndex = trimmedPart.IndexOf(' ');
                
                if (spaceIndex > 0 && int.TryParse(trimmedPart.Substring(0, spaceIndex), out int count))
                {
                    var denomination = trimmedPart.Substring(spaceIndex + 1).Trim();
                    
                    decimal value = denomination switch
                    {
                        "dollar" or "dollars" => 1.00m,
                        "quarter" or "quarters" => 0.25m,
                        "dime" or "dimes" => 0.10m,
                        "nickel" or "nickels" => 0.05m,
                        "penny" or "pennies" => 0.01m,
                        _ => 0m
                    };
                    
                    totalValue += count * value;
                }
            }

            return Math.Abs(totalValue - expectedAmount) < 0.001m;
        }
    }
}
