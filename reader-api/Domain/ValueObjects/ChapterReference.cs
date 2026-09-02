namespace Reader.Api.Domain.ValueObjects;

public sealed record ChapterReference
{
    public ChapterReference(
        ExternalResourceId id,
        string language,
        string? title = null,
        string? volume = null,
        string? number = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("A chapter language is required.", nameof(language));
        }

        Id = id;
        Language = language.Trim();
        Title = Normalize(title);
        Volume = Normalize(volume);
        Number = Normalize(number);
    }

    public ExternalResourceId Id { get; }

    public string Language { get; }

    public string? Title { get; }

    public string? Volume { get; }

    public string? Number { get; }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}