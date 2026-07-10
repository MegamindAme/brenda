using Brenda.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Brenda.Infrastructure.Data;

public class BrendaDbContext : DbContext
{
    public BrendaDbContext(DbContextOptions<BrendaDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlenderVersion> BlenderVersions => Set<BlenderVersion>();

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlenderVersion>(entity =>
        {
            entity.Property(v => v.Version).IsRequired();
            entity.Property(v => v.ExecutablePath).IsRequired();
            entity.HasIndex(v => v.ExecutablePath).IsUnique();
            entity.Ignore(v => v.Series);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.FolderPath).IsRequired();
            entity.HasIndex(p => p.FolderPath).IsUnique();
            entity.HasOne(p => p.PinnedBlenderVersion)
                .WithMany()
                .HasForeignKey(p => p.PinnedBlenderVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
