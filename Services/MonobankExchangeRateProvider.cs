using System.Net;
using System.Text.Json;
using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    public class MonobankExchangeRateProvider : IExchangeRateProvider
    {
        private static readonly Dictionary<int, string> CurrencyCodes = new()
        {
            [840] = "USD",
            [978] = "EUR"
        };

        private const int UahCode = 980;
        private const int MaxAttempts = 3;
        private readonly HttpClient _httpClient;

        public MonobankExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string BankName => "монобанк";

        public async Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(CancellationToken cancellationToken = default)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                var response = await _httpClient.GetAsync("bank/currency", cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < MaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5 * attempt), cancellationToken);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiRates = JsonSerializer.Deserialize<List<MonobankApiResponse>>(json) ?? [];

                return apiRates
                    .Where(r => r.CurrencyCodeB == UahCode && CurrencyCodes.ContainsKey(r.CurrencyCodeA))
                    .Where(r => r.RateBuy.HasValue && r.RateSell.HasValue)
                    .Select(r => new ExchangeRate
                    {
                        BankName = BankName,
                        CurrencyCode = CurrencyCodes[r.CurrencyCodeA],
                        BuyRate = r.RateBuy!.Value,
                        SellRate = r.RateSell!.Value,
                        FetchDate = DateTime.Now
                    })
                    .ToList();
            }

            return [];
        }
    }
}
