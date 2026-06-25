using System.Globalization;
using System.Text.Json;
using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    public abstract class PrivatBankExchangeRateProviderBase : IExchangeRateProvider
    {
        private static readonly string[] SupportedCurrencies = ["USD", "EUR"];
        private readonly HttpClient _httpClient;
        private readonly int _coursId;

        protected PrivatBankExchangeRateProviderBase(HttpClient httpClient, int coursId, string bankName)
        {
            _httpClient = httpClient;
            _coursId = coursId;
            BankName = bankName;
        }

        public string BankName { get; }

        public async Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
                $"p24api/pubinfo?exchange&json&coursid={_coursId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiRates = JsonSerializer.Deserialize<List<PrivatApiResponse>>(json) ?? [];
            var fetchDate = DateTime.Now;

            return apiRates
                .Where(r => SupportedCurrencies.Contains(r.Ccy, StringComparer.OrdinalIgnoreCase))
                .Select(r => new ExchangeRate
                {
                    BankName = BankName,
                    CurrencyCode = r.Ccy.ToUpperInvariant(),
                    BuyRate = ParseRate(r.Buy),
                    SellRate = ParseRate(r.Sale),
                    FetchDate = fetchDate
                })
                .ToList();
        }

        private static decimal ParseRate(string value) =>
            decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public class PrivatBankCardExchangeRateProvider : PrivatBankExchangeRateProviderBase
    {
        public PrivatBankCardExchangeRateProvider(HttpClient httpClient)
            : base(httpClient, 11, "ПриватБанк (безготівковий)")
        {
        }
    }

    public class PrivatBankCashExchangeRateProvider : PrivatBankExchangeRateProviderBase
    {
        public PrivatBankCashExchangeRateProvider(HttpClient httpClient)
            : base(httpClient, 5, "ПриватБанк (готівка)")
        {
        }
    }
}
