using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;

namespace reader_api.Tests;

public class CatalogReferenceTests
{
    [Fact]
    public void MangaReference_WithMissingTitle_ThrowsArgumentException()
    {
        var id = new ExternalResourceId(ExternalCatalogProvider.MangaDex, "manga-123");

        Assert.Throws<ArgumentException>(() => new MangaReference(id, " ", null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChapterReference_WithMissingLanguage_ThrowsArgumentException()
    {
        var id = new ExternalResourceId(ExternalCatalogProvider.MangaDex, "chapter-123");

        Assert.Throws<ArgumentException>(() => new ChapterReference(id, " "));
    }

    [Fact]
    public void ChapterReference_NormalizesOptionalMetadata()
    {
        var chapter = new ChapterReference(
            new ExternalResourceId(ExternalCatalogProvider.MangaDex, "chapter-123"),
            " pt-br ",
            " ",
            " 1 ",
            " 5 ");

        Assert.Equal("pt-br", chapter.Language);
        Assert.Null(chapter.Title);
        Assert.Equal("1", chapter.Volume);
        Assert.Equal("5", chapter.Number);
    }
}