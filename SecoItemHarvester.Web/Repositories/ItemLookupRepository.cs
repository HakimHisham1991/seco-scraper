using Microsoft.EntityFrameworkCore;
using SecoItemHarvester.Web.Data;
using SecoItemHarvester.Web.Dtos;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Web.Repositories;

public class ItemLookupRepository(AppDbContext db) : IItemLookupRepository
{
    public async Task<int> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var count = await db.ItemLookups.CountAsync(cancellationToken);
        if (count == 0)
        {
            return 0;
        }

        await db.ItemLookups.ExecuteDeleteAsync(cancellationToken);
        return count;
    }

    public async Task<int> AddPendingItemsAsync(IEnumerable<string> itemNumbers, CancellationToken cancellationToken = default)
    {
        var normalized = itemNumbers
            .Select(ItemNumberNormalizer.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            return 0;
        }

        var existing = await db.ItemLookups
            .Where(x => normalized.Contains(x.ItemNumber))
            .Select(x => x.ItemNumber)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var toAdd = normalized
            .Where(n => !existingSet.Contains(n))
            .Select(n => new ItemLookup
            {
                ItemNumber = n,
                Status = ItemLookupStatus.Pending,
                CreatedUtc = now,
                UpdatedUtc = now
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            return 0;
        }

        db.ItemLookups.AddRange(toAdd);
        await db.SaveChangesAsync(cancellationToken);
        return toAdd.Count;
    }

    public async Task<IReadOnlyList<ItemLookup>> ClaimPendingBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.ItemLookups
            .Where(x => x.Status == ItemLookupStatus.Pending)
            .OrderBy(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return pending;
        }

        var now = DateTime.UtcNow;
        foreach (var item in pending)
        {
            item.Status = ItemLookupStatus.Processing;
            item.UpdatedUtc = now;
            item.ErrorMessage = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return pending;
    }

    public async Task UpdateResultAsync(long id, ItemLookupResult result, CancellationToken cancellationToken = default)
    {
        var entity = await db.ItemLookups.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.UpdatedUtc = DateTime.UtcNow;
        entity.Source = result.Source;

        if (result.Success && !string.IsNullOrWhiteSpace(result.ItemDescription))
        {
            entity.Status = ItemLookupStatus.Completed;
            entity.ItemDescription = result.ItemDescription.Trim();
            entity.ErrorMessage = null;
        }
        else
        {
            entity.Status = ItemLookupStatus.Failed;
            entity.ErrorMessage = result.Error ?? "Lookup failed.";
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedToPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.ItemLookups
            .Where(x => x.Status == ItemLookupStatus.Failed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ItemLookupStatus.Pending)
                    .SetProperty(x => x.UpdatedUtc, now)
                    .SetProperty(x => x.ErrorMessage, (string?)null),
                cancellationToken);
    }

    public async Task<ProcessingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var counts = await db.ItemLookups
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dto = new ProcessingStatusDto();
        foreach (var row in counts)
        {
            dto.Total += row.Count;
            switch (row.Status)
            {
                case ItemLookupStatus.Pending:
                    dto.Pending = row.Count;
                    break;
                case ItemLookupStatus.Processing:
                    dto.Processing = row.Count;
                    break;
                case ItemLookupStatus.Completed:
                    dto.Completed = row.Count;
                    break;
                case ItemLookupStatus.Failed:
                    dto.Failed = row.Count;
                    break;
            }
        }

        return dto;
    }

    public async Task<IReadOnlyList<ItemLookupDto>> GetResultsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await db.ItemLookups
            .OrderByDescending(x => x.UpdatedUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new ItemLookupDto
            {
                Id = x.Id,
                ItemNumber = x.ItemNumber,
                ItemDescription = x.ItemDescription,
                Status = x.Status.ToString(),
                ErrorMessage = x.ErrorMessage,
                Source = x.Source,
                UpdatedUtc = x.UpdatedUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemLookup>> GetCompletedForExportAsync(CancellationToken cancellationToken = default)
    {
        return await db.ItemLookups
            .Where(x => x.Status == ItemLookupStatus.Completed)
            .OrderBy(x => x.ItemNumber)
            .ToListAsync(cancellationToken);
    }
}
