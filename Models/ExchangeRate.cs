using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRateProject.Models
{
    /// <summary>
    /// Сутність для збереження курсу в базі даних (Entity Framework).
    /// </summary>
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }

        public string BankName { get; set; } = string.Empty;

        public string CurrencyCode { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 4)")]
        public decimal BuyRate { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal SellRate { get; set; }

        public DateTime FetchDate { get; set; }
    }
}
