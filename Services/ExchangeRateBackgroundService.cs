using Microsoft.Extensions.Options;

namespace ExchangeRateProject.Services
{
    public class ExchangeRateBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ExchangeRateSyncOptions _options;
        private readonly ILogger<ExchangeRateBackgroundService> _logger;

        public ExchangeRateBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<ExchangeRateSyncOptions> options,
            ILogger<ExchangeRateBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.AutoSyncEnabled)
            {
                _logger.LogInformation("Автоматичне збереження курсів вимкнено в appsettings.json");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await SyncIfNeededAsync(stoppingToken);

                try
                {
                    await Task.Delay(_options.RefreshInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SyncIfNeededAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var exchangeRateService = scope.ServiceProvider.GetRequiredService<ExchangeRateService>();

                if (!await exchangeRateService.NeedsRefreshAsync(_options.RefreshInterval, cancellationToken))
                {
                    return;
                }

                var result = await exchangeRateService.FetchAndSaveRatesAsync(cancellationToken);
                var removed = await exchangeRateService.CleanupOldRatesAsync(_options.HistoryRetentionDays, cancellationToken);

                _logger.LogInformation(
                    "Автозбереження: {Count} курсів, банки: {Banks}, видалено старих: {Removed}",
                    result.Rates.Count,
                    string.Join(", ", result.SucceededBanks),
                    removed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Помилка фонового збереження курсів у БД");
            }
        }
    }
}