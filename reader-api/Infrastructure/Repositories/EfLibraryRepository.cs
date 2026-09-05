using Microsoft.EntityFrameworkCore;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.ValueObjects;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace Reader.Api.Infrastructure.Repositories;

public sealed class EfLibraryRepository(ReaderDbContext context) : ILibraryRepository
{
    public Task<LibraryItem?> GetByUserAndMangaAsync(Guid userId, ExternalResourceId mangaId, CancellationToken cancellationToken = default) =>
        context.LibraryItems
            .Include(item => item.ReadingProgresses)
            .SingleOrDefaultAsync(
                item => item.UserId == userId
                    && item.Manga.Id.Provider == mangaId.Provider
                    && item.Manga.Id.Value == mangaId.Value,
                cancellationToken);

    public async Task<IReadOnlyList<LibraryItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.LibraryItems
            .Include(item => item.ReadingProgresses)
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LibraryItem item, CancellationToken cancellationToken = default)
    {
        await context.LibraryItems.AddAsync(item, cancellationToken);
    }

    public Task SaveAsync(LibraryItem item, CancellationToken cancellationToken = default)
    {
        if (context.ChangeTracker.Entries<LibraryItem>().All(entry => entry.Entity.Id != item.Id))
        {
            context.LibraryItems.Update(item);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(LibraryItem item, CancellationToken cancellationToken = default)
    {
        context.LibraryItems.Remove(item);
        return Task.CompletedTask;
    }
}
