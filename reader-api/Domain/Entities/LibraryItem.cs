using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Domain.Entities;

public sealed class LibraryItem
{
    private readonly List<ReadingProgress> readingProgresses = [];

    public LibraryItem(Guid id, Guid userId, MangaReference manga, bool isFavorite, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A library item identifier is required.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        Id = id;
        UserId = userId;
        Manga = manga ?? throw new ArgumentNullException(nameof(manga));
        IsFavorite = isFavorite;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    // EF Core materialization.
    private LibraryItem()
    {
        Manga = null!;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public MangaReference Manga { get; }

    public bool IsFavorite { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ReadingProgress> ReadingProgresses => readingProgresses.AsReadOnly();

    public void SetFavorite(bool isFavorite, DateTimeOffset updatedAt)
    {
        IsFavorite = isFavorite;
        UpdatedAt = updatedAt;
    }

    public ReadingProgress RegisterProgress(
        ChapterReference chapter,
        int currentPage,
        int? pageCount,
        DateTimeOffset readAt)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var progress = readingProgresses.SingleOrDefault(item => item.Chapter.Id == chapter.Id);
        if (progress is null)
        {
            progress = new ReadingProgress(Guid.NewGuid(), chapter, currentPage, pageCount, readAt);
            readingProgresses.Add(progress);
        }
        else
        {
            progress.Update(currentPage, pageCount, readAt);
        }

        UpdatedAt = readAt;
        return progress;
    }

    public void MarkChapterCompleted(ExternalResourceId chapterId, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(chapterId);

        var progress = readingProgresses.SingleOrDefault(item => item.Chapter.Id == chapterId)
            ?? throw new InvalidOperationException("The chapter has no reading progress.");

        progress.Complete(completedAt);
        UpdatedAt = completedAt;
    }

    public ReadingProgress? GetResumeProgress() => readingProgresses
        .Where(progress => !progress.IsCompleted)
        .OrderByDescending(progress => progress.LastReadAt)
        .FirstOrDefault()
        ?? readingProgresses.OrderByDescending(progress => progress.LastReadAt).FirstOrDefault();
}