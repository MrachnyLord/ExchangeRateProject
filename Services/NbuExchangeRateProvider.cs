using System.Text.Json;
using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    public class NbuExchangeRateProvider : IExchangeRateProvider
    {
        public static readonly string[] DisplayCurrencies = ["USD", "EUR"];
        public static readonly string[] ConverterCurrencies = ["USD", "EUR"];

        private readonly HttpClient _httpClient;

        public NbuExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string BankName => "НБУ";

        public async Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
                "NBUStatService/v1/statdirectory/exchange?json",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiRates = JsonSerializer.Deserialize<List<NbuApiResponse>>(json) ?? [];
            var fetchDate = DateTime.Now;

            return apiRates
                .Where(r => DisplayCurrencies.Contains(r.Cc, StringComparer.OrdinalIgnoreCase))
                .Select(r => new ExchangeRate
                {
                    BankName = BankName,
                    CurrencyCode = r.Cc.ToUpperInvariant(),
                    BuyRate = r.Rate,
                    SellRate = r.Rate,
                    FetchDate = fetchDate
                })
                .ToList();
        }

        public async Task<Dictionary<string, decimal>> FetchOfficialRatesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
                "NBUStatService/v1/statdirectory/exchange?json",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiRates = JsonSerializer.Deserialize<List<NbuApiResponse>>(json) ?? [];

            return apiRates
                .Where(r => ConverterCurrencies.Contains(r.Cc, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(r => r.Cc.ToUpperInvariant(), r => r.Rate, StringComparer.OrdinalIgnoreCase);
        }
    }
}
