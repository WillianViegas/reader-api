using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Infrastructure.MangaDex;

public sealed class MangaDexCatalogProvider(MangaDexApiClient client, MangaDexOptions options) : ICatalogProvider
{
    public async Task<PagedResultDto<MangaSummaryDto>> SearchAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(query.PageSize, 1, options.MaxPageSize);
        var offset = (query.Page - 1) * limit;
        var title = string.IsNullOrWhiteSpace(query.Title) ? null : $"&title={Uri.EscapeDataString(query.Title.Trim())}";
        var tagIds = (query.TagIds ?? [])
            .Append(query.Category ?? string.Empty)
            .Where(tagId => !string.IsNullOrWhiteSpace(tagId))
            .Select(tagId => tagId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var tags = string.Concat(tagIds.Select(tagId => $"&includedTags[]={Uri.EscapeDataString(tagId)}"));
        var tagMode = tagIds.Length > 0 ? "&includedTagsMode=OR" : string.Empty;
        var response = await client.GetAsync<MangaDexCollectionResponse<MangaDexManga>>(
            $"manga?limit={limit}&offset={offset}&includes[]=cover_art{title}{tags}{tagMode}",
            options.MaxRetryAttempts,
            cancellationToken);

        return new PagedResultDto<MangaSummaryDto>(
            (response?.Data ?? []).Select(manga => MangaDexMapper.ToSummary(manga, options.CoversBaseAddress)).ToList(),
            query.Page,
            limit,
            response?.Total ?? 0);
    }

    public async Task<IReadOnlyList<CatalogTagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync<MangaDexCollectionResponse<MangaDexTag>>(
            "manga/tag?limit=100",
            options.MaxRetryAttempts,
            cancellationToken);

        return (response?.Data ?? [])
            .Select(tag => new CatalogTagDto(
                tag.Id,
                tag.Attributes.Name.GetValueOrDefault("en") ?? tag.Attributes.Name.Values.FirstOrDefault() ?? tag.Id,
                tag.Attributes.Group))
            .OrderBy(tag => tag.Group)
            .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<MangaDetailsDto?> GetDetailsAsync(ExternalResourceId mangaId, CancellationToken cancellationToken = default)
    {
        if (mangaId.Provider is not ExternalCatalogProvider.MangaDex)
        {
            return null;
        }

        var response = await client.GetAsync<MangaDexEntityResponse<MangaDexManga>>(
            $"manga/{Uri.EscapeDataString(mangaId.Value)}?includes[]=cover_art",
            options.MaxRetryAttempts,
            cancellationToken);

        return response?.Data is null
            ? null
            : MangaDexMapper.ToDetails(response.Data, options.CoversBaseAddress);
    }
}
