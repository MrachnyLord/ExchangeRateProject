namespace ExchangeRateProject.Models.ViewModels
{
    public class RateRowViewModel
    {
        public string BankName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public DateTime FetchDate { get; set; }
        public bool IsBestBuy { get; set; }
        public bool IsBestSell { get; set; }
        public bool IsOfficialRate { get; set; }
        public bool IsMarketAverage { get; set; }
    }

    public class IndexPageViewModel
    {
        public List<RateRowViewModel> OfficialRates { get; set; } = [];
        public List<RateRowViewModel> CommercialRates { get; set; } = [];
        public string SelectedCurrency { get; set; } = "USD";
        public List<string> AvailableCurrencies { get; set; } = ["USD", "EUR"];
        public DateTime? LastUpdated { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool HasTodayData { get; set; }
    }

    public class HistoryPageViewModel
    {
        public List<RateRowViewModel> Rates { get; set; } = [];
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public string? SelectedCurrency { get; set; }
        public List<DateTime> AvailableDates { get; set; } = [];
        public List<string> AvailableCurrencies { get; set; } = ["USD", "EUR"];
    }

    public class ConverterPageViewModel
    {
        public static readonly string[] SupportedCurrencies = ["UAH", "USD", "EUR"];

        public static readonly (string Value, string Label)[] AvailableBanks =
        [
            ("nbu", "НБУ"),
            ("monobank", "Монобанк"),
            ("privat", "ПриватБанк")
        ];

        public decimal Amount { get; set; } = 100;
        public string FromCurrency { get; set; } = "USD";
        public string ToCurrency { get; set; } = "UAH";
        public string RateSource { get; set; } = "nbu";
        public decimal? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> AvailableCurrencies { get; set; } = [.. SupportedCurrencies];
    }
}