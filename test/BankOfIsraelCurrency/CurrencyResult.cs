using System;

namespace BankOfIsraelCurrency
{
    public class Currencies
    {
        public ExchangeRate[] ExchangeRates { get; set; }
    }

    public class ExchangeRate
    {
        public string Key { get; set; }
        public float CurrentExchangeRate { get; set; }
        public float CurrentChange { get; set; }
        public int Unit { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}