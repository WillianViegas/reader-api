using Microsoft.EntityFrameworkCore;
using Reader.Api.Domain.Entities;

namespace Reader.Api.Infrastructure.DatabaseConfig;

public sealed class ReaderDbContext(DbContextOptions<ReaderDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<LibraryItem> LibraryItems => Set<LibraryItem>();

    public DbSet<SavedTranslation> SavedTranslations => Set<SavedTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReaderDbContext).Assembly);
    }
}
