namespace CashRegister.Server.Services
{
    public class CashRegisterConfiguration
    {
        public required Dictionary<string, decimal> CurrencyDenominations { get; set; }
        public required string CurrencySymbol { get; set; }
        public required RandomChangeProbabilities RandomProbabilities { get; set; }
    }

    public class RandomChangeProbabilities
    {
        public double SkipDenominationProbability { get; set; } = 0.3;
        public double UsePartialAmountProbability { get; set; } = 0.4;
        public double UseFullAmountProbability { get; set; } = 0.3;
        public int MaxCoinsPerDenomination { get; set; } = 10;
    }
}