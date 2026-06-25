using ExchangeRateProject.Data;
using ExchangeRateProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateProject.Services
{
    public class ExchangeRateService
    {
        private readonly AppDbContext _context;
        private readonly IEnumerable<IExchangeRateProvider> _providers;
        private readonly ILogger<ExchangeRateService> _logger;

        public ExchangeRateService(
            AppDbContext context,
            IEnumerable<IExchangeRateProvider> providers,
            ILogger<ExchangeRateService> logger)
        {
            _context = context;
            _providers = providers;
            _logger = logger;
        }

        public async Task<bool> NeedsRefreshAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            var lastUpdate = await GetTodayLastUpdateTimeAsync(cancellationToken);
            if (!lastUpdate.HasValue)
            {
                return true;
            }

            return DateTime.Now - lastUpdate.Value > maxAge;
        }

        public async Task<FetchRatesResult> FetchAndSaveRatesAsync(CancellationToken cancellationToken = default)
        {
            var snapshotTime = DateTime.Now;
            var allRates = new List<ExchangeRate>();
            var succeededBanks = new List<string>();
            var failedBanks = new List<string>();

            foreach (var provider in _providers)
            {
                if (provider.BankName == "Монобанк")
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }

                try
                {
                    var rates = await provider.FetchRatesAsync(cancellationToken);
                    foreach (var rate in rates)
                    {
                        rate.FetchDate = snapshotTime;
                    }

                    if (rates.Count > 0)
                    {
                        allRates.AddRange(rates);
                        succeededBanks.Add(provider.BankName);
                    }
                    else
                    {
                        failedBanks.Add($"{provider.BankName} (порожня відповідь)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не вдалося отримати курс від {BankName}", provider.BankName);

                    var cached = await GetLatestBankRatesFromTodayAsync(provider.BankName, cancellationToken);
                    if (cached.Count > 0)
                    {
                        foreach (var rate in cached)
                        {
                            rate.FetchDate = snapshotTime;
                            rate.Id = 0;
                        }

                        allRates.AddRange(cached);
                        succeededBanks.Add($"{provider.BankName} (кешовано)");
                    }
                    else
                    {
                        failedBanks.Add(provider.BankName);
                    }
                }
            }

            if (allRates.Count > 0)
            {
                _context.Rates.AddRange(allRates);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new FetchRatesResult
            {
                Rates = allRates,
                SucceededBanks = succeededBanks,
                FailedBanks = failedBanks,
                SnapshotTime = allRates.Count > 0 ? snapshotTime : null
            };
        }

        public async Task<int> CleanupOldRatesAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            if (retentionDays <= 0)
            {
                return 0;
            }

            var threshold = DateTime.Today.AddDays(-retentionDays);
            return await _context.Rates
                .Where(r => r.FetchDate < threshold)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ExchangeRate>> GetTodayLatestRatesAsync(CancellationToken cancellationToken = default)
        {
            return await GetLatestRatesForDayAsync(DateTime.Today, cancellationToken);
        }

        public async Task<IReadOnlyList<ExchangeRate>> GetLatestRatesForDayAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var dayRates = await _context.Rates
                .Where(r => r.FetchDate >= dayStart && r.FetchDate < dayEnd)
                .ToListAsync(cancellationToken);

            return GetLatestSnapshot(dayRates);
        }

        public async Task<DateTime?> GetTodayLastUpdateTimeAsync(CancellationToken cancellationToken = default)
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            return await _context.Rates
                .Where(r => r.FetchDate >= todayStart && r.FetchDate < todayEnd)
                .OrderByDescending(r => r.FetchDate)
                .Select(r => (DateTime?)r.FetchDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DateTime>> GetAvailableHistoryDatesAsync(CancellationToken cancellationToken = default)
        {
            var fetchDates = await _context.Rates
                .Select(r => r.FetchDate)
                .ToListAsync(cancellationToken);

            return fetchDates
                .Select(d => d.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
        }

        public async Task<IReadOnlyList<ExchangeRate>> GetRatesForHistoryDateAsync(
            DateTime date,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            var rates = await GetLatestRatesForDayAsync(date, cancellationToken);

            if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                rates = rates
                    .Where(r => r.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return rates;
        }

        private async Task<List<ExchangeRate>> GetLatestBankRatesFromTodayAsync(
            string bankName,
            CancellationToken cancellationToken)
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var bankRates = await _context.Rates
                .Where(r => r.BankName == bankName && r.FetchDate >= todayStart && r.FetchDate < todayEnd)
                .ToListAsync(cancellationToken);

            return GetLatestPerBankSnapshot(bankRates).ToList();
        }

        private static IReadOnlyList<ExchangeRate> GetLatestSnapshot(List<ExchangeRate> dayRates) =>
            GetLatestPerBankSnapshot(dayRates);

        private static IReadOnlyList<ExchangeRate> GetLatestPerBankSnapshot(List<ExchangeRate> dayRates)
        {
            if (dayRates.Count == 0)
            {
                return [];
            }

            return dayRates
                .GroupBy(r => new { r.BankName, r.CurrencyCode })
                .Select(g => g.OrderByDescending(r => r.FetchDate).First())
                .OrderBy(r => r.BankName == "НБУ" ? 0 : 1)
                .ThenBy(r => r.CurrencyCode)
                .ThenBy(r => r.BankName)
                .ToList();
        }

        public static bool IsOfficialNbuRate(ExchangeRate rate) => rate.BankName == "НБУ";

        public static bool IsNbuSource(ExchangeRate rate) => rate.BankName.StartsWith("НБУ", StringComparison.Ordinal);

        public static BestRatesInfo CalculateBestRates(IEnumerable<ExchangeRate> rates)
        {
            var commercialRates = rates.Where(r => !IsNbuSource(r)).ToList();
            var result = new BestRatesInfo();

            foreach (var currency in commercialRates.Select(r => r.CurrencyCode).Distinct())
            {
                var currencyRates = commercialRates.Where(r => r.CurrencyCode == currency).ToList();

                if (currencyRates.Count == 0)
                {
                    continue;
                }

                var bestBuy = currencyRates.OrderByDescending(r => r.BuyRate).First();
                var bestSell = currencyRates.OrderBy(r => r.SellRate).First();

                result.BestBuyRates[currency] = bestBuy;
                result.BestSellRates[currency] = bestSell;
            }

            return result;
        }
    }

    public class FetchRatesResult
    {
        public IReadOnlyList<ExchangeRate> Rates { get; init; } = [];
        public IReadOnlyList<string> SucceededBanks { get; init; } = [];
        public IReadOnlyList<string> FailedBanks { get; init; } = [];
        public DateTime? SnapshotTime { get; init; }
    }

    public class BestRatesInfo
    {
        public Dictionary<string, ExchangeRate> BestBuyRates { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ExchangeRate> BestSellRates { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}