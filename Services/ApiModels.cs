using System.Text.Json.Serialization;

namespace ExchangeRateProject.Services
{
    public class NbuApiResponse
    {
        [JsonPropertyName("cc")]
        public string Cc { get; set; } = string.Empty;

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("txt")]
        public string Txt { get; set; } = string.Empty;
    }

    public class PrivatApiResponse
    {
        [JsonPropertyName("ccy")]
        public string Ccy { get; set; } = string.Empty;

        [JsonPropertyName("buy")]
        public string Buy { get; set; } = string.Empty;

        [JsonPropertyName("sale")]
        public string Sale { get; set; } = string.Empty;
    }

    public class MonobankApiResponse
    {
        [JsonPropertyName("currencyCodeA")]
        public int CurrencyCodeA { get; set; }

        [JsonPropertyName("currencyCodeB")]
        public int CurrencyCodeB { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("rateBuy")]
        public decimal? RateBuy { get; set; }

        [JsonPropertyName("rateSell")]
        public decimal? RateSell { get; set; }
    }

    public class NbuKursfResponse
    {
        [JsonPropertyName("id_api")]
        public string IdApi { get; set; } = string.Empty;

        [JsonPropertyName("r030")]
        public string R030 { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }
}
