namespace Reader.Api.Infrastructure.MangaDex;

public sealed class MangaDexOptions
{
    public const string SectionName = "MangaDex";

    public string BaseAddress { get; init; } = "https://api.mangadex.org/";

    public string CoversBaseAddress { get; init; } = "https://uploads.mangadex.org/";

    public string UserAgent { get; init; } = "reader-api/1.0";

    public int TimeoutSeconds { get; init; } = 15;

    public int MaxRetryAttempts { get; init; } = 3;

    public int MaxPageSize { get; init; } = 100;
}
