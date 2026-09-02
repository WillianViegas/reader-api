using Reader.Api.Domain.Entities;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.Exceptions;
using Reader.Api.Domain.ValueObjects;

namespace reader_api.Tests;

public class LibraryItemTests
{
    [Fact]
    public void RegisterProgress_WhenLastPageIsRead_CompletesChapter()
    {
        var item = CreateLibraryItem();
        var readAt = DateTimeOffset.UtcNow;

        var progress = item.RegisterProgress(CreateChapter("chapter-1"), 12, 12, readAt);

        Assert.True(progress.IsCompleted);
        Assert.Equal(readAt, progress.CompletedAt);
    }

    [Fact]
    public void RegisterProgress_WhenPageExceedsPageCount_ThrowsDomainException()
    {
        var item = CreateLibraryItem();

        Assert.Throws<InvalidReadingProgressException>(() =>
            item.RegisterProgress(CreateChapter("chapter-1"), 13, 12, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RegisterProgress_ForSameChapter_UpdatesExistingProgress()
    {
        var item = CreateLibraryItem();
        var chapter = CreateChapter("chapter-1");

        item.RegisterProgress(chapter, 2, 12, DateTimeOffset.UtcNow.AddMinutes(-1));
        var progress = item.RegisterProgress(chapter, 5, 12, DateTimeOffset.UtcNow);

        Assert.Single(item.ReadingProgresses);
        Assert.Equal(5, progress.CurrentPage);
    }

    [Fact]
    public void RegisterProgress_WhenChapterWasAlreadyCompleted_PreservesCompletionTimestamp()
    {
        var item = CreateLibraryItem();
        var chapter = CreateChapter("chapter-1");
        var completedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        item.RegisterProgress(chapter, 12, 12, completedAt);
        var progress = item.RegisterProgress(chapter, 12, 12, DateTimeOffset.UtcNow);

        Assert.Equal(completedAt, progress.CompletedAt);
    }

    [Fact]
    public void GetResumeProgress_PrefersMostRecentIncompleteChapter()
    {
        var item = CreateLibraryItem();
        var earliest = DateTimeOffset.UtcNow.AddMinutes(-2);
        var latest = DateTimeOffset.UtcNow;

        item.RegisterProgress(CreateChapter("completed"), 10, 10, latest.AddMinutes(-1));
        var expected = item.RegisterProgress(CreateChapter("in-progress"), 4, 10, latest);
        item.RegisterProgress(CreateChapter("older"), 2, 10, earliest);

        Assert.Equal(expected, item.GetResumeProgress());
    }

    private static LibraryItem CreateLibraryItem() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        new MangaReference(
            new ExternalResourceId(ExternalCatalogProvider.MangaDex, "manga-1"),
            "Manga",
            null,
            null,
            DateTimeOffset.UtcNow),
        false,
        DateTimeOffset.UtcNow);

    private static ChapterReference CreateChapter(string id) => new(
        new ExternalResourceId(ExternalCatalogProvider.MangaDex, id),
        "pt-br");
}