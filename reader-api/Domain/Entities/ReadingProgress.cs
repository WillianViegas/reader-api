using Reader.Api.Domain.Exceptions;
using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Domain.Entities;

public sealed class ReadingProgress
{
    internal ReadingProgress(Guid id, ChapterReference chapter, int currentPage, int? pageCount, DateTimeOffset readAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A reading progress identifier is required.", nameof(id));
        }

        Id = id;
        Chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
        Update(currentPage, pageCount, readAt);
    }

    public Guid Id { get; }

    public ChapterReference Chapter { get; }

    public int CurrentPage { get; private set; }

    public int? PageCount { get; private set; }

    public DateTimeOffset LastReadAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsCompleted => CompletedAt is not null;

    internal void Update(int currentPage, int? pageCount, DateTimeOffset readAt)
    {
        ValidatePage(currentPage, pageCount);

        CurrentPage = currentPage;
        PageCount = pageCount;
        LastReadAt = readAt;

        if (pageCount is not null && currentPage == pageCount)
        {
            CompletedAt ??= readAt;
        }
    }

    internal void Complete(DateTimeOffset completedAt)
    {
        CompletedAt ??= completedAt;
        LastReadAt = completedAt;
    }

    private static void ValidatePage(int currentPage, int? pageCount)
    {
        if (currentPage < 1)
        {
            throw new InvalidReadingProgressException("The current page must be at least 1.");
        }

        if (pageCount is <= 0)
        {
            throw new InvalidReadingProgressException("The page count must be greater than 0 when provided.");
        }

        if (pageCount is not null && currentPage > pageCount)
        {
            throw new InvalidReadingProgressException("The current page cannot exceed the page count.");
        }
    }
}