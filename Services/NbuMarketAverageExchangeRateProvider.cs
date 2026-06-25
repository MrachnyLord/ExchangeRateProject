using System.Text.Json;
using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    /// <summary>
    /// Середньозважений курс готівкового ринку з API НБУ (kursf).
    /// Джерело: bank.gov.ua/NBUStatService/v1/statdirectory/kursf
    /// </summary>
    public class NbuMarketAverageExchangeRateProvider : IExchangeRateProvider
    {
        private static readonly Dictionary<string, string> CurrencyCodes = new()
        {
            ["840"] = "USD",
            ["978"] = "EUR"
        };

        private readonly HttpClient _httpClient;

        public NbuMarketAverageExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string BankName => "НБУ (середній ринковий)";

        public async Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(CancellationToken cancellationToken = default)
        {
            for (var daysBack = 0; daysBack < 7; daysBack++)
            {
                var date = DateTime.Today.AddDays(-daysBack);
                var rates = await TryFetchForDateAsync(date, cancellationToken);
                if (rates.Count > 0)
                {
                    return rates;
                }
            }

            return [];
        }

        private async Task<List<ExchangeRate>> TryFetchForDateAsync(DateTime date, CancellationToken cancellationToken)
        {
            var dateParam = date.ToString("yyyyMMdd");
            var response = await _httpClient.GetAsync(
                $"NBUStatService/v1/statdirectory/kursf?date={dateParam}&period=d&json",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var items = JsonSerializer.Deserialize<List<NbuKursfResponse>>(json) ?? [];

            var result = new List<ExchangeRate>();

            foreach (var (code, currency) in CurrencyCodes)
            {
                var buy = items.FirstOrDefault(i => i.IdApi == "AvgKursBuy" && i.R030 == code)?.Value;
                var sell = items.FirstOrDefault(i => i.IdApi == "AvgKursSel" && i.R030 == code)?.Value;

                if (buy.HasValue && sell.HasValue)
                {
                    result.Add(new ExchangeRate
                    {
                        BankName = BankName,
                        CurrencyCode = currency,
                        BuyRate = buy.Value,
                        SellRate = sell.Value,
                        FetchDate = DateTime.Now
                    });
                }
            }

            return result;
        }
    }
}
