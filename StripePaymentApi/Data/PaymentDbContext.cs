
using Microsoft.EntityFrameworkCore;
using StripePaymentApi.Models;
using System.Reflection.Emit;

namespace StripePaymentApi.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Amount)
                .HasPrecision(18, 2);

            entity.Property(p => p.Currency)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(p => p.ReferenceNo)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.PaymentProvider)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.CustomerEmail)
                .HasMaxLength(255);

            entity.Property(p => p.Description)
                .HasMaxLength(500);

            entity.Property(p => p.StripeSessionId)
                .HasMaxLength(255);

            entity.Property(p => p.StripePaymentIntentId)
                .HasMaxLength(255);

            entity.HasIndex(p => p.ReferenceNo)
                .IsUnique();

            entity.HasIndex(p => p.StripeSessionId);
        });
    }
}

