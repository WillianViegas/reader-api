using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;
using Reader.Api.Infrastructure.MangaDex;

namespace reader_api.Tests;

public class CatalogEndpointTests
{
    [Fact]
    public async Task CatalogEndpoints_WithoutAToken_ReturnUnauthorized()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/manga?title=chainsaw")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/manga/some-id")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/manga/some-id/chapters")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/chapters/some-id/pages")).StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsTheCatalogProviderResults()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/manga?title=chainsaw&pageSize=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<MangaSummaryDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var manga = Assert.Single(result.Items);
        Assert.Equal("manga-1", manga.Id);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetDetails_WhenTheMangaExists_ReturnsIt()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/manga/manga-1");
        var result = await response.Content.ReadFromJsonAsync<MangaDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("manga-1", result.Id);
    }

    [Fact]
    public async Task GetDetails_WhenTheMangaDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(catalog => catalog.MissingManga = true);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/manga/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetChapters_ReturnsTheChapterProviderResults()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/manga/manga-1/chapters");
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ChapterSummaryDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("chapter-1", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetPages_ReturnsTheChapterPageUrls()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/chapters/chapter-1/pages");
        var result = await response.Content.ReadFromJsonAsync<ChapterPagesDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("chapter-1", result.ChapterId);
        Assert.Single(result.PageUrls);
    }

    [Fact]
    public async Task Search_WhenTheCatalogIsUnavailable_ReturnsServiceUnavailableWithProblemDetails()
    {
        using var factory = CreateFactory(catalog => catalog.Unavailable = true);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalog/manga?title=chainsaw");
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("External catalog unavailable", problem);
    }

    private static ReaderApiFactory CreateFactory(Action<StubProviders>? configure = null)
    {
        var stubs = new StubProviders();
        configure?.Invoke(stubs);

        var factory = new ReaderApiFactory();
        factory.ReplaceService<ICatalogProvider>(stubs);
        factory.ReplaceService<IChapterProvider>(stubs);
        _ = factory.Server;

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Reader.Api.Infrastructure.DatabaseConfig.ReaderDbContext>().Database.EnsureCreated();
        return factory;
    }

    private sealed class StubProviders : ICatalogProvider, IChapterProvider
    {
        public bool MissingManga { get; set; }

        public bool Unavailable { get; set; }

        public Task<PagedResultDto<MangaSummaryDto>> SearchAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default)
        {
            ThrowIfUnavailable();
            return Task.FromResult(new PagedResultDto<MangaSummaryDto>(
                [new MangaSummaryDto(ExternalCatalogProvider.MangaDex, "manga-1", "Manga One", null, "pt-br")],
                query.Page,
                query.PageSize,
                1));
        }

        public Task<MangaDetailsDto?> GetDetailsAsync(ExternalResourceId mangaId, CancellationToken cancellationToken = default)
        {
            ThrowIfUnavailable();
            return Task.FromResult(MissingManga
                ? null
                : new MangaDetailsDto(mangaId.Provider, mangaId.Value, "Manga One", null, "pt-br", "Description"));
        }

        public Task<PagedResultDto<ChapterSummaryDto>> GetChaptersAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default)
        {
            ThrowIfUnavailable();
            return Task.FromResult(new PagedResultDto<ChapterSummaryDto>(
                [new ChapterSummaryDto(ExternalCatalogProvider.MangaDex, "chapter-1", "pt-br", null, "1", "1", null)],
                query.Page,
                query.PageSize,
                1));
        }

        public Task<ChapterPagesDto?> GetPagesAsync(ExternalResourceId chapterId, CancellationToken cancellationToken = default)
        {
            ThrowIfUnavailable();
            return Task.FromResult<ChapterPagesDto?>(new ChapterPagesDto(chapterId.Provider, chapterId.Value, ["https://example.test/1.jpg"]));
        }

        private void ThrowIfUnavailable()
        {
            if (Unavailable)
            {
                throw new ExternalCatalogUnavailableException("Simulated outage.");
            }
        }
    }
}
