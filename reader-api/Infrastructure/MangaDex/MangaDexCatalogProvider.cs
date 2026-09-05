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
        var response = await client.GetAsync<MangaDexCollectionResponse<MangaDexManga>>(
            $"manga?limit={limit}&offset={offset}&includes[]=cover_art{title}",
            options.MaxRetryAttempts,
            cancellationToken);

        return new PagedResultDto<MangaSummaryDto>(
            (response?.Data ?? []).Select(manga => MangaDexMapper.ToSummary(manga, options.CoversBaseAddress)).ToList(),
            query.Page,
            limit,
            response?.Total ?? 0);
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
