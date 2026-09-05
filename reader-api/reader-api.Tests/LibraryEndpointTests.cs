using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reader.Api.Application.Dtos;
using Reader.Api.Domain.Enums;
using reader_api.Controllers;

namespace reader_api.Tests;

public class LibraryEndpointTests
{
    private static readonly MangaReferenceDto Manga = new(ExternalCatalogProvider.MangaDex, "manga-1", "Manga One", null, "pt-br");
    private static readonly ChapterReferenceDto Chapter = new(ExternalCatalogProvider.MangaDex, "chapter-1", "pt-br", null, "1", "1");

    [Fact]
    public async Task LibraryEndpoints_WithoutAToken_ReturnUnauthorized()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/library")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/library/manga-1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/library/manga-1/favorite", new SetFavoriteRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/library/continue-reading")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/library/manga-1/progress")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/library/manga-1/progress", new RegisterProgressRequest(Chapter, 1, 10))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/library/manga-1/chapters/chapter-1/complete", null)).StatusCode);
    }

    [Fact]
    public async Task LibraryFlow_AddFavoriteProgressCompleteAndRemove_PersistsAcrossRequests()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var add = await client.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);

        var favorite = await client.PutAsJsonAsync("/api/library/manga-1/favorite", new SetFavoriteRequest(true));
        Assert.True((await favorite.Content.ReadFromJsonAsync<LibraryItemDto>())!.IsFavorite);

        var favorites = await client.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library?favoriteOnly=true");
        Assert.Single(favorites!.Items);

        var progressResponse = await client.PutAsJsonAsync("/api/library/manga-1/progress", new RegisterProgressRequest(Chapter, 4, 10));
        var progress = await progressResponse.Content.ReadFromJsonAsync<ReadingProgressDto>();
        Assert.Equal(4, progress!.CurrentPage);
        Assert.Null(progress.CompletedAt);

        var resume = await client.GetFromJsonAsync<ContinueReadingDto>("/api/library/continue-reading");
        Assert.Equal("manga-1", resume!.Manga.ExternalId);
        Assert.Equal(4, resume.Progress.CurrentPage);

        var progresses = await client.GetFromJsonAsync<List<ReadingProgressDto>>("/api/library/manga-1/progress");
        Assert.Single(progresses!);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/library/manga-1/chapters/chapter-1/complete", null)).StatusCode);

        var completed = (await client.GetFromJsonAsync<List<ReadingProgressDto>>("/api/library/manga-1/progress"))!.Single();
        Assert.NotNull(completed.CompletedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/library/manga-1")).StatusCode);

        var library = await client.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library");
        Assert.Empty(library!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/library/manga-1/progress")).StatusCode);
    }

    [Fact]
    public async Task LibraryFlow_IsScopedToTheAuthenticatedUser()
    {
        using var factory = CreateFactory();
        var owner = await factory.CreateAuthenticatedClientAsync();
        var other = await factory.CreateAuthenticatedClientAsync("other-reader@example.test");

        var add = await owner.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var otherLibrary = await other.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library");
        Assert.Empty(otherLibrary!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync("/api/library/manga-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync("/api/library/manga-1/favorite", new SetFavoriteRequest(true))).StatusCode);
    }

    [Fact]
    public async Task RegisterProgress_WithAnInvalidPage_ReturnsBadRequestWithProblemDetails()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga));

        var response = await client.PutAsJsonAsync("/api/library/manga-1/progress", new RegisterProgressRequest(Chapter, 0, 10));
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Domain rule violation", problem);
    }

    private static ReaderApiFactory CreateFactory()
    {
        var factory = new ReaderApiFactory();
        _ = factory.Server;

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Reader.Api.Infrastructure.DatabaseConfig.ReaderDbContext>().Database.EnsureCreated();
        return factory;
    }
}
