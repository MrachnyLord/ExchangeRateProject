using ExchangeRateProject.Models;

namespace ExchangeRateProject.Services
{
    public interface IExchangeRateProvider
    {
        string BankName { get; }

        Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(CancellationToken cancellationToken = default);
    }
}
