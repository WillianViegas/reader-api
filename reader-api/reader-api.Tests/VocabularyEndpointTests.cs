using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reader.Api.Application.Dtos;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace reader_api.Tests;

public class VocabularyEndpointTests
{
    [Fact]
    public async Task VocabularyEndpoints_SaveListAndDeleteOnlyForAuthenticatedOwner()
    {
        using var factory = new ReaderApiFactory();
        _ = factory.Server;
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ReaderDbContext>().Database.EnsureCreated();
        }

        var owner = await factory.CreateAuthenticatedClientAsync();
        var otherUser = await factory.CreateAuthenticatedClientAsync("another-reader@example.test");
        var request = new SaveVocabularyTranslationRequest
        {
            OriginalText = "Buongiorno",
            TranslatedText = "Bom dia",
            SourceLanguage = "it",
            TargetLanguage = "pt-BR",
            ContextText = "Buongiorno, come stai?",
            MangaExternalId = "manga-1",
            MangaTitle = "Sample manga",
            ChapterExternalId = "chapter-1",
            ChapterTitle = "Chapter 1",
            PageNumber = 12
        };

        var saveResponse = await owner.PostAsJsonAsync("/api/vocabulary", request);
        var saved = await saveResponse.Content.ReadFromJsonAsync<SavedVocabularyTranslationDto>();

        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);
        Assert.NotNull(saveResponse.Headers.Location);
        Assert.NotNull(saved);
        Assert.Equal(request.OriginalText, saved.OriginalText);
        Assert.Equal(request.TranslatedText, saved.TranslatedText);
        Assert.Equal(request.ContextText, saved.ContextText);
        Assert.Equal(request.PageNumber, saved.PageNumber);

        var ownerList = await owner.GetFromJsonAsync<PagedResultDto<SavedVocabularyTranslationDto>>("/api/vocabulary");
        var otherList = await otherUser.GetFromJsonAsync<PagedResultDto<SavedVocabularyTranslationDto>>("/api/vocabulary");
        Assert.Equal(saved.Id, Assert.Single(ownerList!.Items).Id);
        Assert.Empty(otherList!.Items);

        var otherDelete = await otherUser.DeleteAsync($"/api/vocabulary/{saved.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otherDelete.StatusCode);

        var getById = await owner.GetFromJsonAsync<SavedVocabularyTranslationDto>($"/api/vocabulary/{saved.Id}");
        Assert.Equal(saved.Id, getById!.Id);

        var delete = await owner.DeleteAsync($"/api/vocabulary/{saved.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<PagedResultDto<SavedVocabularyTranslationDto>>("/api/vocabulary"))!.Items);
    }

    [Fact]
    public async Task VocabularyEndpoints_RequireAuthentication()
    {
        using var factory = new ReaderApiFactory();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/vocabulary")).StatusCode);

        var response = await client.PostAsJsonAsync("/api/vocabulary", new SaveVocabularyTranslationRequest
        {
            OriginalText = "Hello",
            TranslatedText = "Olá",
            SourceLanguage = "en",
            TargetLanguage = "pt-BR"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}