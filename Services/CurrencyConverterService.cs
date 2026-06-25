using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    public class CurrencyConverterService
    {
        public decimal ConvertViaUah(
            decimal amount,
            string fromCurrency,
            string toCurrency,
            IReadOnlyDictionary<string, decimal> ratesToUah)
        {
            fromCurrency = fromCurrency.ToUpperInvariant();
            toCurrency = toCurrency.ToUpperInvariant();

            if (fromCurrency == toCurrency)
            {
                return amount;
            }

            var amountInUah = ToUah(amount, fromCurrency, ratesToUah);
            return FromUah(amountInUah, toCurrency, ratesToUah);
        }

        public decimal ConvertUsingBankRate(
            decimal amount,
            string fromCurrency,
            string toCurrency,
            IReadOnlyList<ExchangeRate> bankRates)
        {
            fromCurrency = fromCurrency.ToUpperInvariant();
            toCurrency = toCurrency.ToUpperInvariant();

            if (fromCurrency == toCurrency)
            {
                return amount;
            }

            if (bankRates.Count == 0)
            {
                throw new InvalidOperationException("Курси обраного банку недоступні.");
            }

            if (fromCurrency == "UAH")
            {
                var sellRate = GetSellRate(bankRates, toCurrency);
                return amount / sellRate;
            }

            if (toCurrency == "UAH")
            {
                var buyRate = GetBuyRate(bankRates, fromCurrency);
                return amount * buyRate;
            }

            var viaUah = amount * GetBuyRate(bankRates, fromCurrency);
            return viaUah / GetSellRate(bankRates, toCurrency);
        }

        private static decimal ToUah(decimal amount, string currency, IReadOnlyDictionary<string, decimal> ratesToUah)
        {
            if (currency == "UAH")
            {
                return amount;
            }

            if (!ratesToUah.TryGetValue(currency, out var rate))
            {
                throw new InvalidOperationException($"Курс для {currency} не знайдено.");
            }

            return amount * rate;
        }

        private static decimal FromUah(decimal amountInUah, string currency, IReadOnlyDictionary<string, decimal> ratesToUah)
        {
            if (currency == "UAH")
            {
                return amountInUah;
            }

            if (!ratesToUah.TryGetValue(currency, out var rate))
            {
                throw new InvalidOperationException($"Курс для {currency} не знайдено.");
            }

            return amountInUah / rate;
        }

        private static decimal GetBuyRate(IReadOnlyList<ExchangeRate> rates, string currency) =>
            rates.First(r => r.CurrencyCode.Equals(currency, StringComparison.OrdinalIgnoreCase)).BuyRate;

        private static decimal GetSellRate(IReadOnlyList<ExchangeRate> rates, string currency) =>
            rates.First(r => r.CurrencyCode.Equals(currency, StringComparison.OrdinalIgnoreCase)).SellRate;
    }
}