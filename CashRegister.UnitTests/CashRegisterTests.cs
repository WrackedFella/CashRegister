using CashRegister.Server.Services;

namespace CashRegister.UnitTests
{
    public class CashRegisterTests
    {
        [Fact]
        public void CalculateChangeForTransactions_WhenAmountOwedNotDivisibleByThree_ReturnsChangeDescriptions()
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
        public void CalculateChangeForTransactions_WhenAmountOwedDivisibleByThree_ReturnsEmptyArray()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService();
            var transactions = new[]
            {
                "3.00,5.00", // 3.00 is divisible by 3
                "6.00,10.00" // 6.00 is divisible by 3
            };

            // Act
            var actualResults = cashRegisterService.CalculateChangeForTransactions(transactions);

            // Assert
            Assert.Empty(actualResults);
        }

        [Fact]
        public void CalculateChangeForTransactions_WithMixedDivisibilityByThree_ReturnsOnlyNonDivisibleResults()
        {
            // Arrange
            var cashRegisterService = new CashRegisterService();
            var transactions = new[]
            {
                "2.12,3.00", // 2.12 is not divisible by 3
                "3.00,5.00", // 3.00 is divisible by 3 - should be filtered out
                "1.97,2.00"  // 1.97 is not divisible by 3
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
    }
}
