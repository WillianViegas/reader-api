namespace Reader.Api.Domain.Entities;

public sealed class SavedTranslation
{
    private SavedTranslation()
    {
        OriginalText = null!;
        TranslatedText = null!;
        SourceLanguage = null!;
        TargetLanguage = null!;
    }

    public SavedTranslation(
        Guid id,
        Guid userId,
        string originalText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        DateTimeOffset savedAt,
        string? contextText = null,
        string? mangaExternalId = null,
        string? mangaTitle = null,
        string? chapterExternalId = null,
        string? chapterTitle = null,
        int? pageNumber = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("A translation identifier is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("A user identifier is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(originalText)) throw new ArgumentException("Original text is required.", nameof(originalText));
        if (string.IsNullOrWhiteSpace(translatedText)) throw new ArgumentException("Translated text is required.", nameof(translatedText));
        if (string.IsNullOrWhiteSpace(sourceLanguage)) throw new ArgumentException("Source language is required.", nameof(sourceLanguage));
        if (string.IsNullOrWhiteSpace(targetLanguage)) throw new ArgumentException("Target language is required.", nameof(targetLanguage));
        if (sourceLanguage.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and target languages must be different.", nameof(targetLanguage));
        }
        if (pageNumber is <= 0) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be positive.");

        Id = id;
        UserId = userId;
        OriginalText = originalText.Trim();
        TranslatedText = translatedText.Trim();
        SourceLanguage = sourceLanguage.Trim();
        TargetLanguage = targetLanguage.Trim();
        ContextText = Normalize(contextText);
        MangaExternalId = Normalize(mangaExternalId);
        MangaTitle = Normalize(mangaTitle);
        ChapterExternalId = Normalize(chapterExternalId);
        ChapterTitle = Normalize(chapterTitle);
        PageNumber = pageNumber;
        SavedAt = savedAt;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public string OriginalText { get; }

    public string TranslatedText { get; }

    public string SourceLanguage { get; }

    public string TargetLanguage { get; }

    public string? ContextText { get; }

    public string? MangaExternalId { get; }

    public string? MangaTitle { get; }

    public string? ChapterExternalId { get; }

    public string? ChapterTitle { get; }

    public int? PageNumber { get; }

    public DateTimeOffset SavedAt { get; }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}