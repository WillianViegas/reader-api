using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Infrastructure.MangaDex;

public sealed class MangaDexChapterProvider(MangaDexApiClient client, MangaDexOptions options) : IChapterProvider
{
    private const string AtHomeDomain = "mangadex.network";

    public async Task<PagedResultDto<ChapterSummaryDto>> GetChaptersAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Provider is not ExternalCatalogProvider.MangaDex)
        {
            return new PagedResultDto<ChapterSummaryDto>([], query.Page, query.PageSize, 0);
        }

        var limit = Math.Clamp(query.PageSize, 1, options.MaxPageSize);
        var offset = (query.Page - 1) * limit;
        var response = await client.GetAsync<MangaDexCollectionResponse<MangaDexChapter>>(
            $"manga/{Uri.EscapeDataString(query.MangaId)}/feed?limit={limit}&offset={offset}&order[volume]=asc&order[chapter]=asc",
            options.MaxRetryAttempts,
            cancellationToken);

        // Chapters hosted externally (official simulpubs) have no pages on MangaDex@Home.
        var readable = (response?.Data ?? [])
            .Where(chapter => chapter.Attributes.ExternalUrl is null && chapter.Attributes.Pages > 0)
            .Select(MangaDexMapper.ToSummary)
            .ToList();

        return new PagedResultDto<ChapterSummaryDto>(readable, query.Page, limit, response?.Total ?? 0);
    }

    public async Task<ChapterPagesDto?> GetPagesAsync(ExternalResourceId chapterId, CancellationToken cancellationToken = default)
    {
        if (chapterId.Provider is not ExternalCatalogProvider.MangaDex)
        {
            return null;
        }

        var response = await client.GetAsync<MangaDexAtHomeResponse>(
            $"at-home/server/{Uri.EscapeDataString(chapterId.Value)}",
            options.MaxRetryAttempts,
            cancellationToken);

        return response is null || string.IsNullOrWhiteSpace(response.BaseUrl)
            ? null
            : MangaDexMapper.ToPages(chapterId.Value, response);
    }

    public async Task<ChapterPageImageDto?> GetPageImageAsync(
        ExternalResourceId chapterId,
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        if (chapterId.Provider is not ExternalCatalogProvider.MangaDex || pageIndex < 0) return null;

        var response = await client.GetAsync<MangaDexAtHomeResponse>(
            $"at-home/server/{Uri.EscapeDataString(chapterId.Value)}",
            options.MaxRetryAttempts,
            cancellationToken);
        if (response is null || !Uri.TryCreate(response.BaseUrl, UriKind.Absolute, out var baseUri) || !IsAllowedAtHomeHost(baseUri))
        {
            return null;
        }

        var files = response.Chapter.Data.Count > 0
            ? response.Chapter.Data
            : response.Chapter.DataSaver;
        if (pageIndex >= files.Count || string.IsNullOrWhiteSpace(response.Chapter.Hash)) return null;

        var imageUri = new Uri(
            $"{baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/')}/data/{Uri.EscapeDataString(response.Chapter.Hash)}/{Uri.EscapeDataString(files[pageIndex])}",
            UriKind.Absolute);
        return await client.GetImageAsync(imageUri, cancellationToken);
    }

    private static bool IsAllowedAtHomeHost(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps &&
        uri.IsDefaultPort &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        (uri.Host.Equals(AtHomeDomain, StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith($".{AtHomeDomain}", StringComparison.OrdinalIgnoreCase));
}
