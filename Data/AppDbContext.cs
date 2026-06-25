using ExchangeRateProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ExchangeRate> Rates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.Property(e => e.BankName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
                entity.Property(e => e.BuyRate).HasColumnType("decimal(18,4)");
                entity.Property(e => e.SellRate).HasColumnType("decimal(18,4)");

                entity.HasIndex(e => e.FetchDate);
                entity.HasIndex(e => new { e.BankName, e.CurrencyCode, e.FetchDate });
            });
        }
    }
}