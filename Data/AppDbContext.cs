using Microsoft.EntityFrameworkCore;
using MidtransQrisApi.Models;

namespace MidtransQrisApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.OrderId)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}