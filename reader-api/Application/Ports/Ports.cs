using Reader.Api.Application.Dtos;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Application.Ports;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByExternalSubjectAsync(string externalSubject, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        GetByExternalSubjectAsync(email, cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

public interface ILibraryRepository
{
    Task<LibraryItem?> GetByUserAndMangaAsync(Guid userId, ExternalResourceId mangaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LibraryItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(LibraryItem item, CancellationToken cancellationToken = default);

    Task SaveAsync(LibraryItem item, CancellationToken cancellationToken = default);

    Task RemoveAsync(LibraryItem item, CancellationToken cancellationToken = default);
}

public interface ISavedTranslationRepository
{
    Task AddAsync(SavedTranslation translation, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SavedTranslation> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SavedTranslation?> GetByUserAndIdAsync(Guid userId, Guid translationId, CancellationToken cancellationToken = default);

    Task RemoveAsync(SavedTranslation translation, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid UserId { get; }
}

public interface ICatalogProvider
{
    Task<PagedResultDto<MangaSummaryDto>> SearchAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogTagDto>> GetTagsAsync(CancellationToken cancellationToken = default);

    Task<MangaDetailsDto?> GetDetailsAsync(ExternalResourceId mangaId, CancellationToken cancellationToken = default);
}

public interface IChapterProvider
{
    Task<PagedResultDto<ChapterSummaryDto>> GetChaptersAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default);

    Task<ChapterPagesDto?> GetPagesAsync(ExternalResourceId chapterId, CancellationToken cancellationToken = default);

    Task<ChapterPageImageDto?> GetPageImageAsync(ExternalResourceId chapterId, int pageIndex, CancellationToken cancellationToken = default);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}