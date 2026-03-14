using AlpineBits.GuestRequestProxy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlpineBits.GuestRequestProxy.Data.Repositories;

public sealed class GuestRequestLogRepository(AlpineBitsDbContext dbContext) : IGuestRequestLogRepository
{
    public async Task AddAsync(GuestRequestLog log, CancellationToken cancellationToken)
    {
        dbContext.GuestRequestLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<GuestRequestLog>> GetPendingRequestsAsync(int tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.GuestRequestLogs
            .Where(x => x.TenantId == tenantId && x.Status == "Pending")
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(List<int> logIds, CancellationToken cancellationToken)
    {
        var logs = await dbContext.GuestRequestLogs
            .Where(x => logIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            log.Status = "Processed";
            log.ProcessedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}


public sealed class GuestRequestLogRepositoryFake() : IGuestRequestLogRepository
{
    public async Task AddAsync(GuestRequestLog log, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public async Task<List<GuestRequestLog>> GetPendingRequestsAsync(int tenantId, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new List<GuestRequestLog>());
    }

    public async Task MarkAsProcessedAsync(List<int> logIds, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

    }
}
