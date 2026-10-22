using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Enums;
using reader_api.Controllers;

namespace reader_api.Tests;

public class LibraryEndpointTests
{
    private static readonly MangaReferenceDto Manga = new(ExternalCatalogProvider.MangaDex, "manga-1", "Manga One", null, "pt-br");
    private static readonly ChapterReferenceDto Chapter = new(ExternalCatalogProvider.MangaDex, "chapter-1", "pt-br", null, "1", "1");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
        Assert.True((await favorite.Content.ReadFromJsonAsync<LibraryItemDto>(JsonOptions))!.IsFavorite);

        var favorites = await client.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library?favoriteOnly=true", JsonOptions);
        Assert.Single(favorites!.Items);

        var progressResponse = await client.PutAsJsonAsync("/api/library/manga-1/progress", new RegisterProgressRequest(Chapter, 4, 10));
        var progress = await progressResponse.Content.ReadFromJsonAsync<ReadingProgressDto>(JsonOptions);
        Assert.Equal(4, progress!.CurrentPage);
        Assert.Null(progress.CompletedAt);

        var resume = await client.GetFromJsonAsync<ContinueReadingDto>("/api/library/continue-reading", JsonOptions);
        Assert.Equal("manga-1", resume!.Manga.ExternalId);
        Assert.Equal(4, resume.Progress.CurrentPage);

        var progresses = await client.GetFromJsonAsync<List<ReadingProgressDto>>("/api/library/manga-1/progress", JsonOptions);
        Assert.Single(progresses!);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/library/manga-1/chapters/chapter-1/complete", null)).StatusCode);

        var completed = (await client.GetFromJsonAsync<List<ReadingProgressDto>>("/api/library/manga-1/progress", JsonOptions))!.Single();
        Assert.NotNull(completed.CompletedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/library/manga-1")).StatusCode);

        var library = await client.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library", JsonOptions);
        Assert.Empty(library!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/library/manga-1/progress")).StatusCode);
    }

        [Fact]
        public async Task Add_WithTheWebClientStringEnumPayload_ReturnsCreated()
        {
                using var factory = CreateFactory();
                var client = await factory.CreateAuthenticatedClientAsync();
                const string requestBody = """
                        {
                            "manga": {
                                "provider": "MangaDex",
                                "externalId": "manga-string-provider",
                                "title": "Manga One",
                                "coverUrl": null,
                                "originalLanguage": "pt-br"
                            },
                            "isFavorite": false
                        }
                        """;

                var response = await client.PostAsync("/api/library", new StringContent(requestBody, Encoding.UTF8, "application/json"));

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

    [Fact]
    public async Task LibraryFlow_IsScopedToTheAuthenticatedUser()
    {
        using var factory = CreateFactory();
        var owner = await factory.CreateAuthenticatedClientAsync();
        var other = await factory.CreateAuthenticatedClientAsync("other-reader@example.test");

        var add = await owner.PostAsJsonAsync("/api/library", new AddLibraryItemRequest(Manga));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var otherLibrary = await other.GetFromJsonAsync<PagedResultDto<LibraryItemDto>>("/api/library", JsonOptions);
        Assert.Empty(otherLibrary!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync("/api/library/manga-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync("/api/library/manga-1/favorite", new SetFavoriteRequest(true))).StatusCode);
    }

    [Fact]
    public async Task PdfImport_ReadsOnlyForOwnerAndRemovesStoredFileWithLibraryItem()
    {
        var storage = new InMemoryPdfStorageProvider();
        using var factory = CreateFactory(storage);
        var owner = await factory.CreateAuthenticatedClientAsync();

        using var form = CreatePdfForm("My local book", "%PDF-1.7\nexample PDF content");
        var upload = await owner.PostAsync("/api/library/pdfs", form);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        var item = await upload.Content.ReadFromJsonAsync<LibraryItemDto>(JsonOptions);
        Assert.Equal(ExternalCatalogProvider.UploadedPdf, item!.Manga.Provider);
        Assert.Equal("My local book", item.Manga.Title);
        Assert.Equal($"/api/library/pdfs/{item.Manga.ExternalId}", upload.Headers.Location?.AbsolutePath);

        var read = await owner.GetAsync($"/api/library/pdfs/{item.Manga.ExternalId}");
        Assert.Equal("application/pdf", read.Content.Headers.ContentType?.MediaType);
        Assert.Equal("%PDF-1.7\nexample PDF content", Encoding.UTF8.GetString(await read.Content.ReadAsByteArrayAsync()));

        var other = await factory.CreateAuthenticatedClientAsync("other-reader@example.test");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/library/pdfs/{item.Manga.ExternalId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/library/pdfs/{item.Manga.ExternalId}")).StatusCode);
        Assert.Empty(storage.Documents);
    }

    [Fact]
    public async Task PdfImport_RejectsFilesWithoutPdfSignature()
    {
        var storage = new InMemoryPdfStorageProvider();
        using var factory = CreateFactory(storage);
        var client = await factory.CreateAuthenticatedClientAsync();

        using var form = CreatePdfForm("Not a PDF", "plain text");
        var response = await client.PostAsync("/api/library/pdfs", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(storage.Documents);
    }

    [Fact]
    public async Task PdfImport_StoresCoverAndTracksReadingProgress()
    {
        var storage = new InMemoryPdfStorageProvider();
        using var factory = CreateFactory(storage);
        var client = await factory.CreateAuthenticatedClientAsync();

        using var form = CreatePdfForm("Comic", "%PDF-1.7\nbody");
        var cover = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x01]);
        cover.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(cover, "Cover", "cover.jpg");
        var upload = await client.PostAsync("/api/library/pdfs", form);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var item = await upload.Content.ReadFromJsonAsync<LibraryItemDto>(JsonOptions);
        var id = item!.Manga.ExternalId;
        Assert.Equal($"/api/library/pdfs/{id}/cover", item.Manga.CoverUrl);

        var coverResponse = await client.GetAsync($"/api/library/pdfs/{id}/cover");
        Assert.Equal("image/jpeg", coverResponse.Content.Headers.ContentType?.MediaType);

        var save = await client.PutAsJsonAsync($"/api/library/pdfs/{id}/progress", new RegisterPdfProgressRequest(3, 10));
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var progress = await client.GetFromJsonAsync<List<ReadingProgressDto>>($"/api/library/pdfs/{id}/progress", JsonOptions);
        Assert.Equal(3, Assert.Single(progress!).CurrentPage);

        var resume = await client.GetFromJsonAsync<ContinueReadingDto>("/api/library/continue-reading", JsonOptions);
        Assert.Equal(ExternalCatalogProvider.UploadedPdf, resume!.Manga.Provider);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/library/pdfs/{id}")).StatusCode);
        Assert.Empty(storage.Covers);
    }

    [Fact]
    public async Task PdfImport_RejectsNonJpegCover()
    {
        var storage = new InMemoryPdfStorageProvider();
        using var factory = CreateFactory(storage);
        var client = await factory.CreateAuthenticatedClientAsync();

        using var form = CreatePdfForm("Comic", "%PDF-1.7\nbody");
        form.Add(new ByteArrayContent([1, 2, 3, 4]), "Cover", "cover.jpg");
        var response = await client.PostAsync("/api/library/pdfs", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(storage.Documents);
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

    private static MultipartFormDataContent CreatePdfForm(string title, string content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), "Title");
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(file, "File", "book.pdf");
        return form;
    }

    private static ReaderApiFactory CreateFactory(IPdfStorageProvider? pdfStorage = null)
    {
        var factory = new ReaderApiFactory();
        if (pdfStorage is not null)
        {
            factory.ReplaceService(pdfStorage);
        }

        _ = factory.Server;

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Reader.Api.Infrastructure.DatabaseConfig.ReaderDbContext>().Database.EnsureCreated();
        return factory;
    }

    private sealed class InMemoryPdfStorageProvider : IPdfStorageProvider
    {
        public Dictionary<(Guid UserId, Guid PdfId), byte[]> Documents { get; } = [];

        public async Task StoreAsync(Guid userId, Guid pdfId, Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Documents.Add((userId, pdfId), buffer.ToArray());
        }

        public Task<Stream?> OpenReadAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default)
        {
            Stream? content = Documents.TryGetValue((userId, pdfId), out var bytes)
                ? new MemoryStream(bytes, writable: false)
                : null;
            return Task.FromResult(content);
        }

        public Task DeleteAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default)
        {
            Documents.Remove((userId, pdfId));
            Covers.Remove((userId, pdfId));
            return Task.CompletedTask;
        }

        public Dictionary<(Guid UserId, Guid PdfId), byte[]> Covers { get; } = [];

        public async Task StoreCoverAsync(Guid userId, Guid pdfId, Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Covers[(userId, pdfId)] = buffer.ToArray();
        }

        public Task<Stream?> OpenCoverAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default)
        {
            Stream? content = Covers.TryGetValue((userId, pdfId), out var bytes)
                ? new MemoryStream(bytes, writable: false)
                : null;
            return Task.FromResult(content);
        }
    }
}
