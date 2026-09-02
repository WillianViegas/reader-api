using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Enums;

namespace Reader.Api.Application.UseCases;

public sealed record GetUserProfileQuery;

public sealed record GetLibraryQuery(bool FavoriteOnly = false, int Page = 1, int PageSize = 20);

public sealed record GetContinueReadingQuery;

public sealed record GetReadingProgressQuery(ExternalCatalogProvider Provider, string MangaId);

public sealed record SearchCatalogQuery(string Title, int Page = 1, int PageSize = 20);

public sealed record GetMangaDetailsQuery(ExternalCatalogProvider Provider, string MangaId);

public sealed record GetMangaChaptersQuery(ExternalCatalogProvider Provider, string MangaId, int Page = 1, int PageSize = 20);

public sealed record GetChapterPagesQuery(ExternalCatalogProvider Provider, string ChapterId);

public sealed class GetUserProfileHandler(IUserRepository users, ICurrentUser currentUser)
{
    public async Task<UserProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("The current user was not found.");

        return ApplicationMapper.ToDto(user);
    }
}

public sealed class GetLibraryHandler(ILibraryRepository library, ICurrentUser currentUser)
{
    public async Task<PagedResultDto<LibraryItemDto>> HandleAsync(GetLibraryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page and page size must be positive.");
        }

        var items = await library.GetByUserAsync(currentUser.UserId, cancellationToken);
        var filtered = items.Where(item => !query.FavoriteOnly || item.IsFavorite).OrderByDescending(item => item.UpdatedAt).ToList();
        return new PagedResultDto<LibraryItemDto>(
            filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(ApplicationMapper.ToDto).ToList(),
            query.Page,
            query.PageSize,
            filtered.Count);
    }
}

public sealed class GetContinueReadingHandler(ILibraryRepository library, ICurrentUser currentUser)
{
    public async Task<ContinueReadingDto?> HandleAsync(GetContinueReadingQuery query, CancellationToken cancellationToken = default)
    {
        var candidate = (await library.GetByUserAsync(currentUser.UserId, cancellationToken))
            .Select(item => new { Item = item, Progress = item.GetResumeProgress() })
            .Where(item => item.Progress is not null)
            .OrderByDescending(item => !item.Progress!.IsCompleted)
            .ThenByDescending(item => item.Progress!.LastReadAt)
            .FirstOrDefault();

        return candidate is null ? null : new ContinueReadingDto(candidate.Item.Id, ApplicationMapper.ToDto(candidate.Item.Manga), ApplicationMapper.ToDto(candidate.Progress!));
    }
}

public sealed class GetReadingProgressHandler(ILibraryRepository library, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<ReadingProgressDto>> HandleAsync(GetReadingProgressQuery query, CancellationToken cancellationToken = default)
    {
        var item = await library.GetByUserAndMangaAsync(currentUser.UserId, ApplicationMapper.ToExternalId(query.Provider, query.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        return item.ReadingProgresses.OrderByDescending(progress => progress.LastReadAt).Select(ApplicationMapper.ToDto).ToList();
    }
}

public sealed class SearchCatalogHandler(ICatalogProvider catalog)
{
    public Task<PagedResultDto<MangaSummaryDto>> HandleAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default) =>
        catalog.SearchAsync(query, cancellationToken);
}

public sealed class GetMangaDetailsHandler(ICatalogProvider catalog)
{
    public Task<MangaDetailsDto?> HandleAsync(GetMangaDetailsQuery query, CancellationToken cancellationToken = default) =>
        catalog.GetDetailsAsync(ApplicationMapper.ToExternalId(query.Provider, query.MangaId), cancellationToken);
}

public sealed class GetMangaChaptersHandler(IChapterProvider chapters)
{
    public Task<PagedResultDto<ChapterSummaryDto>> HandleAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default) =>
        chapters.GetChaptersAsync(query, cancellationToken);
}

public sealed class GetChapterPagesHandler(IChapterProvider chapters)
{
    public Task<ChapterPagesDto?> HandleAsync(GetChapterPagesQuery query, CancellationToken cancellationToken = default) =>
        chapters.GetPagesAsync(ApplicationMapper.ToExternalId(query.Provider, query.ChapterId), cancellationToken);
}