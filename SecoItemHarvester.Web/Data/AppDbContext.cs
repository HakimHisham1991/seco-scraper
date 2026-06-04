using Microsoft.EntityFrameworkCore;
using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ItemLookup> ItemLookups => Set<ItemLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemLookup>(entity =>
        {
            entity.ToTable("ItemLookup");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ItemNumber).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.ItemNumber);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ItemDescription).HasMaxLength(512);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.Source).HasMaxLength(64);
        });
    }
}
