using ExchangeRateProject.Models.ViewModels;
using ExchangeRateProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExchangeRateProject.Pages
{
    public class HistoryModel : PageModel
    {
        private readonly ExchangeRateService _exchangeRateService;

        public HistoryModel(ExchangeRateService exchangeRateService)
        {
            _exchangeRateService = exchangeRateService;
        }

        public HistoryPageViewModel ViewModel { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Currency { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            ViewModel.AvailableDates = (await _exchangeRateService.GetAvailableHistoryDatesAsync(cancellationToken)).ToList();
            ViewModel.SelectedCurrency = Currency;
            ViewModel.SelectedDate = Date ?? ViewModel.AvailableDates.FirstOrDefault();

            if (ViewModel.SelectedDate == default)
            {
                return;
            }

            var rates = await _exchangeRateService.GetRatesForHistoryDateAsync(
                ViewModel.SelectedDate,
                Currency,
                cancellationToken);

            var bestRates = ExchangeRateService.CalculateBestRates(rates);

            ViewModel.Rates = rates.Select(r => new RateRowViewModel
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
