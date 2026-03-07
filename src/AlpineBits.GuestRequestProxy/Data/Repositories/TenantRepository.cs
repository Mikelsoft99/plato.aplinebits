using AlpineBits.GuestRequestProxy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlpineBits.GuestRequestProxy.Data.Repositories;

public sealed class TenantRepository(AlpineBitsDbContext dbContext) : ITenantRepository
{
    public Task<HotelTenant?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        return dbContext.HotelTenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApiKey == apiKey && x.IsActive, cancellationToken);
    }

    public Task<HotelTenant?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return dbContext.HotelTenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username && x.IsActive, cancellationToken);
    }

    public Task<HotelTenant?> GetFirstActiveTenantAsync(CancellationToken cancellationToken)
    {
        return dbContext.HotelTenants
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
