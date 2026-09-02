namespace Reader.Api.Domain.ValueObjects;

public sealed record MangaReference
{
    public MangaReference(
        ExternalResourceId id,
        string title,
        string? coverUrl,
        string? originalLanguage,
        DateTimeOffset snapshotUpdatedAt)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A manga title is required.", nameof(title));
        }

        Id = id;
        Title = title.Trim();
        CoverUrl = coverUrl?.Trim();
        OriginalLanguage = originalLanguage?.Trim();
        SnapshotUpdatedAt = snapshotUpdatedAt;
    }

    public ExternalResourceId Id { get; }

    public string Title { get; }

    public string? CoverUrl { get; }

    public string? OriginalLanguage { get; }

    public DateTimeOffset SnapshotUpdatedAt { get; }
}