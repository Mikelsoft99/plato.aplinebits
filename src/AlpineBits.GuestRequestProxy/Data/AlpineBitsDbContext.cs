using AlpineBits.GuestRequestProxy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlpineBits.GuestRequestProxy.Data;

public sealed class AlpineBitsDbContext(DbContextOptions<AlpineBitsDbContext> options) : DbContext(options)
{
    public DbSet<HotelTenant> HotelTenants => Set<HotelTenant>();
    public DbSet<GuestRequestLog> GuestRequestLogs => Set<GuestRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HotelTenant>(entity =>
        {
            entity.ToTable("HotelTenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HotelCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ApiKey).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TargetUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(200);
            entity.Property(x => x.Password).HasMaxLength(200);
            entity.HasIndex(x => x.ApiKey).IsUnique();
            entity.HasIndex(x => x.HotelCode).IsUnique();

            entity.HasData(new HotelTenant
            {
                Id = 1,
                HotelCode = "HOTEL001",
                Name = "Seed Hotel",
                ApiKey = "replace-me",
                TargetUrl = "https://asa.example.com/alpinebits",
                Username = "",
                Password = "",
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<GuestRequestLog>(entity =>
        {
            entity.ToTable("GuestRequestLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Direction).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequestJson);
            entity.Property(x => x.RequestXml);
            entity.Property(x => x.ResponseBody);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.GuestRequestLogs)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.TenantId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.Status);
        });
    }
}
