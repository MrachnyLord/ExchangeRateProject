using ExchangeRateProject.Models;
using ExchangeRateProject.Models.ViewModels;
using ExchangeRateProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace ExchangeRateProject.Pages
{
    public class IndexModel : PageModel
    {
        private static readonly string[] AllowedCurrencies = ["USD", "EUR"];

        private readonly ExchangeRateService _exchangeRateService;
        private readonly ExchangeRateSyncOptions _syncOptions;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ExchangeRateService exchangeRateService,
            IOptions<ExchangeRateSyncOptions> syncOptions,
            ILogger<IndexModel> logger)
        {
            _exchangeRateService = exchangeRateService;
            _syncOptions = syncOptions.Value;
            _logger = logger;
        }

        public IndexPageViewModel ViewModel { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Currency { get; set; } = "USD";

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            ViewModel.SelectedCurrency = NormalizeCurrency(Currency);
            await LoadTodayRatesAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
        {
            ViewModel.SelectedCurrency = NormalizeCurrency(Currency);

            try
            {
                var result = await _exchangeRateService.FetchAndSaveRatesAsync(cancellationToken);
                await _exchangeRateService.CleanupOldRatesAsync(
                    _syncOptions.HistoryRetentionDays,
                    cancellationToken);

                if (result.Rates.Count == 0)
                {
                    ViewModel.ErrorMessage = "Не вдалося завантажити курси. Перевірте інтернет.";
                }
                else
                {
                    ViewModel.SuccessMessage =
                        $"Курси оновлено о {result.SnapshotTime:dd.MM.yyyy HH:mm:ss}. " +
                        $"Джерела: {string.Join(", ", result.SucceededBanks)}.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка ручного оновлення курсів");
                ViewModel.ErrorMessage = "Помилка оновлення курсів.";
            }

            await LoadTodayRatesAsync(cancellationToken);
            return Page();
        }

        private async Task LoadTodayRatesAsync(CancellationToken cancellationToken)
        {
            var rates = await _exchangeRateService.GetTodayLatestRatesAsync(cancellationToken);
            ViewModel.LastUpdated = await _exchangeRateService.GetTodayLastUpdateTimeAsync(cancellationToken);
            ViewModel.HasTodayData = rates.Count > 0;

            var mapped = MapToViewModel(rates);
            var currency = ViewModel.SelectedCurrency;

            ViewModel.OfficialRates = mapped
                .Where(r => (r.IsOfficialRate || r.IsMarketAverage) && r.CurrencyCode == currency)
                .ToList();

            ViewModel.CommercialRates = mapped
                .Where(r => !r.IsOfficialRate && !r.IsMarketAverage && r.CurrencyCode == currency)
                .ToList();

            if (!ViewModel.HasTodayData && string.IsNullOrEmpty(ViewModel.ErrorMessage))
            {
                ViewModel.ErrorMessage = "Курси ще не завантажені. Натисніть «Оновити».";
            }
        }

        private static string NormalizeCurrency(string? currency) =>
            AllowedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase)
                ? currency!.ToUpperInvariant()
                : "USD";

        private static List<RateRowViewModel> MapToViewModel(IReadOnlyList<ExchangeRate> rates)
        {
            var bestRates = ExchangeRateService.CalculateBestRates(rates);

            return rates.Select(r => new RateRowViewModel
            {
                BankName = r.BankName,
                CurrencyCode = r.CurrencyCode,
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                FetchDate = r.FetchDate,
                IsOfficialRate = ExchangeRateService.IsOfficialNbuRate(r),
                IsMarketAverage = r.BankName.StartsWith("НБУ (", StringComparison.Ordinal),
                IsBestBuy = bestRates.BestBuyRates.TryGetValue(r.CurrencyCode, out var bestBuy) && bestBuy.BankName == r.BankName,
                IsBestSell = bestRates.BestSellRates.TryGetValue(r.CurrencyCode, out var bestSell) && bestSell.BankName == r.BankName
            }).ToList();
        }
    }
}