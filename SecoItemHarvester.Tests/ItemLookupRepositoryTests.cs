using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecoItemHarvester.Web.Data;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Repositories;

namespace SecoItemHarvester.Tests;

public class ItemLookupRepositoryTests
{
    [Fact]
    public async Task AddPendingItemsAsync_SkipsDuplicates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var repository = new ItemLookupRepository(db);
        var first = await repository.AddPendingItemsAsync(["02679365", "02679366"]);
        var second = await repository.AddPendingItemsAsync(["02679365"]);

        first.Should().Be(2);
        second.Should().Be(0);
        (await db.ItemLookups.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllRows()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var repository = new ItemLookupRepository(db);
        await repository.AddPendingItemsAsync(["02679365", "02679366"]);
        var removed = await repository.ClearAllAsync();

        removed.Should().Be(2);
        (await db.ItemLookups.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ClaimPendingBatchAsync_MarksProcessing()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var repository = new ItemLookupRepository(db);
        await repository.AddPendingItemsAsync(["02679365"]);

        var batch = await repository.ClaimPendingBatchAsync(10);

        batch.Should().HaveCount(1);
        batch[0].Status.Should().Be(ItemLookupStatus.Processing);
    }
}
