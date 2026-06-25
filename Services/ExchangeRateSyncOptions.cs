namespace ExchangeRateProject.Services
{
    public class ExchangeRateSyncOptions
    {
        public const string SectionName = "ExchangeRateSync";

        public bool AutoSyncEnabled { get; set; } = true;

        public int RefreshIntervalMinutes { get; set; } = 60;

        public int HistoryRetentionDays { get; set; } = 90;

        public int StartupDelaySeconds { get; set; } = 3;

        public TimeSpan RefreshInterval => TimeSpan.FromMinutes(RefreshIntervalMinutes);
    }
}