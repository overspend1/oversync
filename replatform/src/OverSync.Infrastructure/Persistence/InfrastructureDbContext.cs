using Microsoft.EntityFrameworkCore;
using OverSync.Infrastructure.Persistence.Entities;

namespace OverSync.Infrastructure.Persistence;

public sealed class InfrastructureDbContext : DbContext
{
    public InfrastructureDbContext(DbContextOptions<InfrastructureDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<ManifestEntity> Manifests => Set<ManifestEntity>();
    public DbSet<CommitEntity> Commits => Set<CommitEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Token);
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(x => x.DeviceId);
            entity.HasIndex(x => new { x.UserId, x.VaultId });
        });

        modelBuilder.Entity<ManifestEntity>(entity =>
        {
            entity.ToTable("manifests");
            entity.HasKey(x => x.VaultId);
        });

        modelBuilder.Entity<CommitEntity>(entity =>
        {
            entity.ToTable("commits");
            entity.HasKey(x => x.CommitId);
            entity.HasIndex(x => new { x.UserId, x.VaultId, x.Version });
        });
    }
}
