using System.Text.Json.Serialization;

namespace Reader.Api.Infrastructure.MangaDex;

// Internal transport contracts for the MangaDex API. These types must never leak
// through the Application DTOs (anti-corruption layer).

internal sealed class MangaDexCollectionResponse<T>
{
    [JsonPropertyName("result")]
    public string Result { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public List<T> Data { get; init; } = [];

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }
}

internal sealed class MangaDexEntityResponse<T>
{
    [JsonPropertyName("result")]
    public string Result { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

internal sealed class MangaDexManga
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public MangaDexMangaAttributes Attributes { get; init; } = new();

    [JsonPropertyName("relationships")]
    public List<MangaDexRelationship> Relationships { get; init; } = [];
}

internal sealed class MangaDexMangaAttributes
{
    [JsonPropertyName("title")]
    public Dictionary<string, string> Title { get; init; } = [];

    [JsonPropertyName("altTitles")]
    public List<Dictionary<string, string>> AltTitles { get; init; } = [];

    [JsonPropertyName("description")]
    public Dictionary<string, string> Description { get; init; } = [];

    [JsonPropertyName("originalLanguage")]
    public string? OriginalLanguage { get; init; }
}

internal sealed class MangaDexRelationship
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public MangaDexCoverAttributes? Attributes { get; init; }
}

internal sealed class MangaDexCoverAttributes
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }
}

internal sealed class MangaDexChapter
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public MangaDexChapterAttributes Attributes { get; init; } = new();
}

internal sealed class MangaDexChapterAttributes
{
    [JsonPropertyName("volume")]
    public string? Volume { get; init; }

    [JsonPropertyName("chapter")]
    public string? Number { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("translatedLanguage")]
    public string? TranslatedLanguage { get; init; }

    [JsonPropertyName("externalUrl")]
    public string? ExternalUrl { get; init; }

    [JsonPropertyName("publishAt")]
    public DateTimeOffset? PublishAt { get; init; }

    [JsonPropertyName("pages")]
    public int Pages { get; init; }
}

internal sealed class MangaDexAtHomeResponse
{
    [JsonPropertyName("result")]
    public string Result { get; init; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; init; } = string.Empty;

    [JsonPropertyName("chapter")]
    public MangaDexAtHomeChapter Chapter { get; init; } = new();
}

internal sealed class MangaDexAtHomeChapter
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public List<string> Data { get; init; } = [];

    [JsonPropertyName("dataSaver")]
    public List<string> DataSaver { get; init; } = [];
}
