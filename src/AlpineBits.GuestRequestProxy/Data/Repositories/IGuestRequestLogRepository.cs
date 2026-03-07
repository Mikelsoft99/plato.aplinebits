using AlpineBits.GuestRequestProxy.Data.Entities;

namespace AlpineBits.GuestRequestProxy.Data.Repositories;

public interface IGuestRequestLogRepository
{
    Task AddAsync(GuestRequestLog log, CancellationToken cancellationToken);
    Task<List<GuestRequestLog>> GetPendingRequestsAsync(int tenantId, CancellationToken cancellationToken);
    Task MarkAsProcessedAsync(List<int> logIds, CancellationToken cancellationToken);
}
