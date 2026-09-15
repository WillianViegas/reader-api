using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Reader.Api.Application.UseCases;

public sealed record GetUserProfileQuery;

public sealed record GetLibraryQuery(bool FavoriteOnly = false, int Page = 1, int PageSize = 20);

public sealed record GetContinueReadingQuery;

public sealed record GetReadingProgressQuery(ExternalCatalogProvider Provider, string MangaId);

public sealed record SearchCatalogQuery(string? Title, int Page = 1, int PageSize = 20, string? Category = null);

public sealed record GetMangaDetailsQuery(ExternalCatalogProvider Provider, string MangaId);

public sealed record GetMangaChaptersQuery(ExternalCatalogProvider Provider, string MangaId, int Page = 1, int PageSize = 20);

public sealed record GetChapterPagesQuery(ExternalCatalogProvider Provider, string ChapterId);

public sealed class GetUserProfileHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetUserProfileHandler>? logger = null)
{
    public async Task<UserProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting profile for user {UserId}", currentUser.UserId);
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("The current user was not found.");

        logger?.LogInformation("Found profile for user {UserId}", user.Id);
        return ApplicationMapper.ToDto(user);
    }
}

public sealed class GetLibraryHandler(
    ILibraryRepository library,
    ICurrentUser currentUser,
    ILogger<GetLibraryHandler>? logger = null)
{
    public async Task<PagedResultDto<LibraryItemDto>> HandleAsync(GetLibraryQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting library for user {UserId}, favoriteOnly {FavoriteOnly}, page {Page}, pageSize {PageSize}", currentUser.UserId, query.FavoriteOnly, query.Page, query.PageSize);
        if (query.Page < 1 || query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page and page size must be positive.");
        }

        var items = await library.GetByUserAsync(currentUser.UserId, cancellationToken);
        var filtered = items.Where(item => !query.FavoriteOnly || item.IsFavorite).OrderByDescending(item => item.UpdatedAt).ToList();
        var result = new PagedResultDto<LibraryItemDto>(
            filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(ApplicationMapper.ToDto).ToList(),
            query.Page,
            query.PageSize,
            filtered.Count);
        logger?.LogInformation("Found {LibraryItemCount} library items for user {UserId}", result.TotalCount, currentUser.UserId);
        return result;
    }
}

public sealed class GetContinueReadingHandler(
    ILibraryRepository library,
    ICurrentUser currentUser,
    ILogger<GetContinueReadingHandler>? logger = null)
{
    public async Task<ContinueReadingDto?> HandleAsync(GetContinueReadingQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting continue-reading item for user {UserId}", currentUser.UserId);
        var candidate = (await library.GetByUserAsync(currentUser.UserId, cancellationToken))
            .Select(item => new { Item = item, Progress = item.GetResumeProgress() })
            .Where(item => item.Progress is not null)
            .OrderByDescending(item => !item.Progress!.IsCompleted)
            .ThenByDescending(item => item.Progress!.LastReadAt)
            .FirstOrDefault();

        if (candidate is null)
        {
            logger?.LogInformation("No continue-reading item found for user {UserId}", currentUser.UserId);
            return null;
        }

        logger?.LogInformation("Found continue-reading library item {LibraryItemId} for user {UserId}", candidate.Item.Id, currentUser.UserId);
        return new ContinueReadingDto(candidate.Item.Id, ApplicationMapper.ToDto(candidate.Item.Manga), ApplicationMapper.ToDto(candidate.Progress!));
    }
}

public sealed class GetReadingProgressHandler(
    ILibraryRepository library,
    ICurrentUser currentUser,
    ILogger<GetReadingProgressHandler>? logger = null)
{
    public async Task<IReadOnlyList<ReadingProgressDto>> HandleAsync(GetReadingProgressQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting progress for manga {MangaProvider}/{MangaId} and user {UserId}", query.Provider, query.MangaId, currentUser.UserId);
        var item = await library.GetByUserAndMangaAsync(currentUser.UserId, ApplicationMapper.ToExternalId(query.Provider, query.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        var progress = item.ReadingProgresses.OrderByDescending(progress => progress.LastReadAt).Select(ApplicationMapper.ToDto).ToList();
        logger?.LogInformation("Found {ProgressCount} progress entries for library item {LibraryItemId}", progress.Count, item.Id);
        return progress;
    }
}

public sealed class SearchCatalogHandler(ICatalogProvider catalog, ILogger<SearchCatalogHandler>? logger = null)
{
    public async Task<PagedResultDto<MangaSummaryDto>> HandleAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Searching catalog, page {Page}, pageSize {PageSize}, category supplied {HasCategory}", query.Page, query.PageSize, !string.IsNullOrWhiteSpace(query.Category));
        var result = await catalog.SearchAsync(query, cancellationToken);
        logger?.LogInformation("Catalog search returned {MangaCount} manga", result.TotalCount);
        return result;
    }
}

public sealed class GetMangaDetailsHandler(ICatalogProvider catalog, ILogger<GetMangaDetailsHandler>? logger = null)
{
    public async Task<MangaDetailsDto?> HandleAsync(GetMangaDetailsQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting manga details for {MangaProvider}/{MangaId}", query.Provider, query.MangaId);
        var result = await catalog.GetDetailsAsync(ApplicationMapper.ToExternalId(query.Provider, query.MangaId), cancellationToken);
        logger?.LogInformation("Manga details found {Found} for {MangaProvider}/{MangaId}", result is not null, query.Provider, query.MangaId);
        return result;
    }
}

public sealed class GetMangaChaptersHandler(IChapterProvider chapters, ILogger<GetMangaChaptersHandler>? logger = null)
{
    public async Task<PagedResultDto<ChapterSummaryDto>> HandleAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting chapters for manga {MangaProvider}/{MangaId}, page {Page}, pageSize {PageSize}", query.Provider, query.MangaId, query.Page, query.PageSize);
        var result = await chapters.GetChaptersAsync(query, cancellationToken);
        logger?.LogInformation("Found {ChapterCount} chapters for manga {MangaProvider}/{MangaId}", result.TotalCount, query.Provider, query.MangaId);
        return result;
    }
}

public sealed class GetChapterPagesHandler(IChapterProvider chapters, ILogger<GetChapterPagesHandler>? logger = null)
{
    public async Task<ChapterPagesDto?> HandleAsync(GetChapterPagesQuery query, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Getting pages for chapter {ChapterProvider}/{ChapterId}", query.Provider, query.ChapterId);
        var result = await chapters.GetPagesAsync(ApplicationMapper.ToExternalId(query.Provider, query.ChapterId), cancellationToken);
        logger?.LogInformation("Chapter pages found {Found} for {ChapterProvider}/{ChapterId}", result is not null, query.Provider, query.ChapterId);
        return result;
    }
}