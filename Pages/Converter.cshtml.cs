using ExchangeRateProject.Models;
using ExchangeRateProject.Models.ViewModels;
using ExchangeRateProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExchangeRateProject.Pages
{
    public class ConverterModel : PageModel
    {
        private readonly NbuExchangeRateProvider _nbuProvider;
        private readonly ExchangeRateService _exchangeRateService;
        private readonly CurrencyConverterService _converterService;

        public ConverterModel(
            NbuExchangeRateProvider nbuProvider,
            ExchangeRateService exchangeRateService,
            CurrencyConverterService converterService)
        {
            _nbuProvider = nbuProvider;
            _exchangeRateService = exchangeRateService;
            _converterService = converterService;
        }

        public ConverterPageViewModel ViewModel { get; set; } = new();

        [BindProperty]
        public decimal Amount { get; set; } = 100;

        [BindProperty]
        public string FromCurrency { get; set; } = "USD";

        [BindProperty]
        public string ToCurrency { get; set; } = "UAH";

        [BindProperty]
        public string RateSource { get; set; } = "nbu";

        public void OnGet()
        {
            FillViewModel();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            FillViewModel();

            if (Amount <= 0)
            {
                ViewModel.ErrorMessage = "Сума повинна бути більше нуля.";
                return Page();
            }

            if (!ConverterPageViewModel.SupportedCurrencies.Contains(FromCurrency, StringComparer.OrdinalIgnoreCase) ||
                !ConverterPageViewModel.SupportedCurrencies.Contains(ToCurrency, StringComparer.OrdinalIgnoreCase))
            {
                ViewModel.ErrorMessage = "Обрана валюта не підтримується.";
                return Page();
            }

            try
            {
                if (RateSource == "nbu")
                {
                    var nbuRates = await _nbuProvider.FetchOfficialRatesAsync(cancellationToken);
                    ViewModel.Result = _converterService.ConvertViaUah(
                        Amount, FromCurrency, ToCurrency, nbuRates);
                }
                else
                {
                    var rates = await _exchangeRateService.GetTodayLatestRatesAsync(cancellationToken);
                    var bankRates = FilterRatesBySource(rates, RateSource);

                    if (bankRates.Count == 0)
                    {
                        var bankLabel = ResolveBankLabel(RateSource);
                        ViewModel.ErrorMessage = $"Курси банку «{bankLabel}» ще не завантажені. Відкрийте головну сторінку та оновіть дані.";
                        return Page();
                    }

                    ViewModel.Result = _converterService.ConvertUsingBankRate(
                        Amount, FromCurrency, ToCurrency, bankRates);
                }
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = ex.Message;
            }

            return Page();
        }

        private void FillViewModel()
        {
            ViewModel.Amount = Amount;
            ViewModel.FromCurrency = FromCurrency.ToUpperInvariant();
            ViewModel.ToCurrency = ToCurrency.ToUpperInvariant();
            ViewModel.RateSource = RateSource;
            ViewModel.AvailableCurrencies = [.. ConverterPageViewModel.SupportedCurrencies];
        }

        private static string ResolveBankLabel(string rateSource) => rateSource switch
        {
            "monobank" => "монобанк",
            "privat" => "ПриватБанк",
            _ => "банк"
        };

        private static List<ExchangeRate> FilterRatesBySource(IReadOnlyList<ExchangeRate> rates, string rateSource)
        {
            return rateSource switch
            {
                "monobank" => rates
                    .Where(r => r.BankName.Equals("монобанк", StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                "privat" => rates
                    .Where(r => r.BankName.StartsWith("ПриватБанк", StringComparison.Ordinal))
                    .ToList(),
                _ => []
            };
        }
    }
}