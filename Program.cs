using ExchangeRateProject.Data;
using ExchangeRateProject.Services;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateProject
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<ExchangeRateSyncOptions>(
                builder.Configuration.GetSection(ExchangeRateSyncOptions.SectionName));

            builder.Services.AddRazorPages();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddHttpClient<NbuExchangeRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://bank.gov.ua/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<NbuMarketAverageExchangeRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://bank.gov.ua/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<PrivatBankCardExchangeRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.privatbank.ua/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<PrivatBankCashExchangeRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.privatbank.ua/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<MonobankExchangeRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.monobank.ua/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddScoped<IExchangeRateProvider>(sp => sp.GetRequiredService<NbuExchangeRateProvider>());
            builder.Services.AddScoped<IExchangeRateProvider>(sp => sp.GetRequiredService<NbuMarketAverageExchangeRateProvider>());
            builder.Services.AddScoped<IExchangeRateProvider>(sp => sp.GetRequiredService<PrivatBankCardExchangeRateProvider>());
            builder.Services.AddScoped<IExchangeRateProvider>(sp => sp.GetRequiredService<PrivatBankCashExchangeRateProvider>());
            builder.Services.AddScoped<IExchangeRateProvider>(sp => sp.GetRequiredService<MonobankExchangeRateProvider>());
            builder.Services.AddScoped<ExchangeRateService>();
            builder.Services.AddScoped<CurrencyConverterService>();
            builder.Services.AddHostedService<ExchangeRateBackgroundService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if(!db.Database.CanConnect())
                {
                    db.Database.Migrate();
                }
                var syncOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExchangeRateSyncOptions>>().Value;
                var exchangeRateService = scope.ServiceProvider.GetRequiredService<ExchangeRateService>();

                if (await exchangeRateService.NeedsRefreshAsync(syncOptions.RefreshInterval))
                {
                    await exchangeRateService.FetchAndSaveRatesAsync();
                    await exchangeRateService.CleanupOldRatesAsync(syncOptions.HistoryRetentionDays);
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}