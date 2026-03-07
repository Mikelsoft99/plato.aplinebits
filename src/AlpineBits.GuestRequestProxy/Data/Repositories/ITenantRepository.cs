using AlpineBits.GuestRequestProxy.Data.Entities;

namespace AlpineBits.GuestRequestProxy.Data.Repositories;

public interface ITenantRepository
{
    Task<HotelTenant?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken);
    Task<HotelTenant?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<HotelTenant?> GetFirstActiveTenantAsync(CancellationToken cancellationToken);
}
