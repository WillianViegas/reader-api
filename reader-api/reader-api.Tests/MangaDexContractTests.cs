using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;
using Reader.Api.Infrastructure.MangaDex;

namespace reader_api.Tests;

public class MangaDexContractTests
{
    private const string MangaId = "a77742b1-befd-49a4-bff5-1ad4e6b0ef7b";
    private const string ChapterId = "16b99098-f939-45f5-abc7-f4dc1e5e222a";

    private static readonly MangaDexOptions Options = new();

    [Fact]
    public async Task Search_MapsRecordedMangaCollectionToStableDtos()
    {
        var provider = new MangaDexCatalogProvider(CreateClient(_ => Json(HttpStatusCode.OK, SearchJson)), Options);

        var result = await provider.SearchAsync(new SearchCatalogQuery("chainsaw", Page: 2, PageSize: 1));

        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(18, result.TotalCount);

        var manga = Assert.Single(result.Items);
        Assert.Equal(ExternalCatalogProvider.MangaDex, manga.Provider);
        Assert.Equal(MangaId, manga.Id);
        Assert.Equal("Chainsaw Man", manga.Title);
        Assert.Equal("ja", manga.OriginalLanguage);
        Assert.Equal($"https://uploads.mangadex.org/covers/{MangaId}/6e518bd1-5f20-446b-8832-bfe6bf74834b.jpg.512.jpg", manga.CoverUrl);
    }

    [Fact]
    public async Task GetDetails_MapsRecordedMangaEntity()
    {
        var provider = new MangaDexCatalogProvider(CreateClient(_ => Json(HttpStatusCode.OK, DetailsJson)), Options);

        var result = await provider.GetDetailsAsync(new ExternalResourceId(ExternalCatalogProvider.MangaDex, MangaId));

        Assert.NotNull(result);
        Assert.Equal(MangaId, result.Id);
        Assert.Equal("Chainsaw Man", result.Title);
        Assert.Equal("ja", result.OriginalLanguage);
        Assert.Equal("Broke young man + chainsaw dog demon = Chainsaw Man!", result.Description);
        Assert.Equal($"https://uploads.mangadex.org/covers/{MangaId}/6e518bd1-5f20-446b-8832-bfe6bf74834b.jpg.512.jpg", result.CoverUrl);
    }

    [Fact]
    public async Task GetChapters_MapsFeedAndExcludesExternallyHostedChapters()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, FeedJson));
        var provider = new MangaDexChapterProvider(CreateClient(handler), Options);

        var result = await provider.GetChaptersAsync(new GetMangaChaptersQuery(ExternalCatalogProvider.MangaDex, MangaId));

        Assert.Equal(468, result.TotalCount);

        var chapter = Assert.Single(result.Items);
        Assert.Equal(ChapterId, chapter.Id);
        Assert.Equal("pt-br", chapter.Language);
        Assert.Equal("Caxoeiro & Motosseirra", chapter.Title);
        Assert.Equal("1", chapter.Volume);
        Assert.Equal("1", chapter.Number);
        Assert.Equal(DateTimeOffset.Parse("2022-04-01T15:22:01+00:00"), chapter.PublishedAt);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("order[volume]=asc", request.RequestUri!.Query);
        Assert.Contains("order[chapter]=asc", request.RequestUri.Query);
    }

    [Fact]
    public async Task GetPages_MapsRecordedAtHomeResponseToPageUrls()
    {
        var provider = new MangaDexChapterProvider(CreateClient(_ => Json(HttpStatusCode.OK, AtHomeJson)), Options);

        var result = await provider.GetPagesAsync(new ExternalResourceId(ExternalCatalogProvider.MangaDex, ChapterId));

        Assert.NotNull(result);
        Assert.Equal(ChapterId, result.ChapterId);
        Assert.Equal(ExternalCatalogProvider.MangaDex, result.Provider);
        Assert.Equal(
            ["https://cmdxd98sb0x3yprd.mangadex.network/data/749d8b7339dcd0924f4f28da8a9c259b/1-ad5e4e24e79de2cf7383b5f8dddf3dd282c0c6d58da525850513a2c6b30d878c.jpg"],
            result.PageUrls);
    }

    [Fact]
    public async Task GetPages_WhenDataIsEmpty_FallsBackToDataSaver()
    {
        const string json = """{"result":"ok","baseUrl":"https://node.mangadex.network","chapter":{"hash":"abc","data":[],"dataSaver":["1-saver.jpg"]}}""";
        var provider = new MangaDexChapterProvider(CreateClient(_ => Json(HttpStatusCode.OK, json)), Options);

        var result = await provider.GetPagesAsync(new ExternalResourceId(ExternalCatalogProvider.MangaDex, ChapterId));

        Assert.NotNull(result);
        Assert.Equal(["https://node.mangadex.network/data/abc/1-saver.jpg"], result.PageUrls);
    }

    [Fact]
    public async Task GetDetails_WhenTheExternalResourceIsMissing_ReturnsNullWithoutThrowing()
    {
        var provider = new MangaDexCatalogProvider(CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)), Options);

        var result = await provider.GetDetailsAsync(new ExternalResourceId(ExternalCatalogProvider.MangaDex, MangaId));

        Assert.Null(result);
    }

    [Fact]
    public async Task Search_WhenTheApiKeepsReturning503_RetriesAndMapsToExternalCatalogUnavailable()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var provider = new MangaDexCatalogProvider(CreateClient(handler), new MangaDexOptions { MaxRetryAttempts = 2 });

        var exception = await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => provider.SearchAsync(new SearchCatalogQuery("chainsaw")));

        Assert.Equal("ExternalCatalogUnavailable", ExternalCatalogUnavailableException.Code);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Search_WhenTheRequestTimesOut_RetriesAndMapsToExternalCatalogUnavailable()
    {
        var handler = new RecordingHandler(_ => throw new TaskCanceledException("Simulated timeout."));
        var provider = new MangaDexCatalogProvider(CreateClient(handler), new MangaDexOptions { MaxRetryAttempts = 2 });

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => provider.SearchAsync(new SearchCatalogQuery("chainsaw")));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Search_WhenATransientFailureIsFollowedBySuccess_ReturnsTheMappedResult()
    {
        var attempts = 0;
        var handler = new RecordingHandler(_ =>
        {
            attempts++;
            return attempts == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : Json(HttpStatusCode.OK, SearchJson);
        });
        var provider = new MangaDexCatalogProvider(CreateClient(handler), Options);

        var result = await provider.SearchAsync(new SearchCatalogQuery("chainsaw"));

        Assert.Single(result.Items);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task GetAsync_SendsTheConfiguredUserAgentAndRelativeUri()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, SearchJson));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mangadex.org/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("reader-api/1.0");

        var client = new MangaDexApiClient(httpClient, NullLogger<MangaDexApiClient>.Instance);
        var provider = new MangaDexCatalogProvider(client, Options);

        await provider.SearchAsync(new SearchCatalogQuery("chainsaw", PageSize: 200));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("reader-api/1.0", request.Headers.UserAgent.ToString());
        Assert.Equal("https://api.mangadex.org/", request.RequestUri!.GetLeftPart(UriPartial.Authority) + "/");
        Assert.Contains("title=chainsaw", request.RequestUri.Query);
        Assert.Contains("limit=100", request.RequestUri.Query);
        Assert.Contains("offset=0", request.RequestUri.Query);
        Assert.Contains("includes[]=cover_art", request.RequestUri.Query);
    }

    private static MangaDexApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        CreateClient(new RecordingHandler(responder));

    private static MangaDexApiClient CreateClient(RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mangadex.org/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("reader-api/1.0");

        return new MangaDexApiClient(httpClient, NullLogger<MangaDexApiClient>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }

    // Recorded from GET /manga?title=chainsaw&limit=1&includes[]=cover_art on 2026-09-04 (trimmed).
    private const string SearchJson = """
        {"result":"ok","response":"collection","data":[{"id":"a77742b1-befd-49a4-bff5-1ad4e6b0ef7b","type":"manga","attributes":{"title":{"ja-ro":"Chainsaw Man"},"altTitles":[{"en":"Chainsaw Man"}],"description":{"en":"Broke young man + chainsaw dog demon = Chainsaw Man!"},"originalLanguage":"ja"},"relationships":[{"id":"23f4ba42-b58f-4216-b4cb-186e5506e1cf","type":"cover_art","attributes":{"fileName":"6e518bd1-5f20-446b-8832-bfe6bf74834b.jpg"}}]}],"limit":1,"offset":0,"total":18}
        """;

    // Recorded from GET /manga/{id}?includes[]=cover_art on 2026-09-04 (trimmed).
    private const string DetailsJson = """
        {"result":"ok","response":"entity","data":{"id":"a77742b1-befd-49a4-bff5-1ad4e6b0ef7b","type":"manga","attributes":{"title":{"ja-ro":"Chainsaw Man"},"altTitles":[{"en":"Chainsaw Man"}],"description":{"en":"Broke young man + chainsaw dog demon = Chainsaw Man!"},"originalLanguage":"ja"},"relationships":[{"id":"23f4ba42-b58f-4216-b4cb-186e5506e1cf","type":"cover_art","attributes":{"fileName":"6e518bd1-5f20-446b-8832-bfe6bf74834b.jpg"}}]}}
        """;

    // Recorded from GET /manga/{id}/feed?limit=2&translatedLanguage[]=pt-br&translatedLanguage[]=en&order[chapter]=asc
    // on 2026-09-04. The second entry is hosted externally (externalUrl set, zero pages) and must be excluded.
    private const string FeedJson = """
        {"result":"ok","response":"collection","data":[{"id":"16b99098-f939-45f5-abc7-f4dc1e5e222a","type":"chapter","attributes":{"volume":"1","chapter":"1","title":"Caxoeiro & Motosseirra","translatedLanguage":"pt-br","externalUrl":null,"publishAt":"2022-04-01T15:22:01+00:00","readableAt":"2022-04-01T15:22:01+00:00","pages":55},"relationships":[{"id":"a77742b1-befd-49a4-bff5-1ad4e6b0ef7b","type":"manga"}]},{"id":"6f2b5712-2461-48fd-a519-c2c9cb93f0b1","type":"chapter","attributes":{"volume":"1","chapter":"1","title":null,"translatedLanguage":"en","externalUrl":"https://viz.com/shonenjump/-/chapter/17489","publishAt":"2024-05-01T15:29:06+00:00","readableAt":"2024-05-01T15:29:06+00:00","pages":0},"relationships":[]}],"limit":2,"offset":0,"total":468}
        """;

    // Recorded from GET /at-home/server/{chapterId} on 2026-09-04 (only the first page kept).
    private const string AtHomeJson = """
        {"result":"ok","baseUrl":"https://cmdxd98sb0x3yprd.mangadex.network","chapter":{"hash":"749d8b7339dcd0924f4f28da8a9c259b","data":["1-ad5e4e24e79de2cf7383b5f8dddf3dd282c0c6d58da525850513a2c6b30d878c.jpg"],"dataSaver":["1-saver.jpg"]}}
        """;
}
