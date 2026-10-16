using Microsoft.EntityFrameworkCore;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Entities;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace Reader.Api.Infrastructure.Repositories;

public sealed class EfSavedTranslationRepository(ReaderDbContext context) : ISavedTranslationRepository
{
    public Task AddAsync(SavedTranslation translation, CancellationToken cancellationToken = default) =>
        context.SavedTranslations.AddAsync(translation, cancellationToken).AsTask();

    public async Task<(IReadOnlyList<SavedTranslation> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userTranslations = await context.SavedTranslations
            .AsNoTracking()
            .Where(translation => translation.UserId == userId)
            .ToListAsync(cancellationToken);
        var totalCount = userTranslations.Count;
        var items = userTranslations
            .OrderByDescending(translation => translation.SavedAt)
            .ThenByDescending(translation => translation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount);
    }

    public Task<SavedTranslation?> GetByUserAndIdAsync(
        Guid userId,
        Guid translationId,
        CancellationToken cancellationToken = default) =>
        context.SavedTranslations.SingleOrDefaultAsync(
            translation => translation.UserId == userId && translation.Id == translationId,
            cancellationToken);

    public Task RemoveAsync(SavedTranslation translation, CancellationToken cancellationToken = default)
    {
        context.SavedTranslations.Remove(translation);
        return Task.CompletedTask;
    }
}