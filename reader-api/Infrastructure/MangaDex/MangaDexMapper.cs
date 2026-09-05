using Reader.Api.Application.Dtos;
using Reader.Api.Domain.Enums;

namespace Reader.Api.Infrastructure.MangaDex;

internal static class MangaDexMapper
{
    public static MangaSummaryDto ToSummary(MangaDexManga manga, string coversBaseAddress) => new(
        ExternalCatalogProvider.MangaDex,
        manga.Id,
        ResolveTitle(manga),
        CoverUrl(manga, coversBaseAddress),
        Normalize(manga.Attributes.OriginalLanguage));

    public static MangaDetailsDto ToDetails(MangaDexManga manga, string coversBaseAddress) => new(
        ExternalCatalogProvider.MangaDex,
        manga.Id,
        ResolveTitle(manga),
        CoverUrl(manga, coversBaseAddress),
        Normalize(manga.Attributes.OriginalLanguage),
        ResolveLocalizedText(manga.Attributes.Description));

    public static ChapterSummaryDto ToSummary(MangaDexChapter chapter) => new(
        ExternalCatalogProvider.MangaDex,
        chapter.Id,
        Normalize(chapter.Attributes.TranslatedLanguage) ?? string.Empty,
        Normalize(chapter.Attributes.Title),
        Normalize(chapter.Attributes.Volume),
        Normalize(chapter.Attributes.Number),
        chapter.Attributes.PublishAt);

    public static ChapterPagesDto ToPages(string chapterId, MangaDexAtHomeResponse response)
    {
        var files = response.Chapter.Data.Count > 0
            ? response.Chapter.Data
            : response.Chapter.DataSaver;

        var baseUrl = response.BaseUrl.TrimEnd('/');
        var urls = files
            .Select(file => $"{baseUrl}/data/{response.Chapter.Hash}/{file}")
            .ToList();

        return new ChapterPagesDto(ExternalCatalogProvider.MangaDex, chapterId, urls);
    }

    private static string ResolveTitle(MangaDexManga manga)
    {
        var title = ResolveLocalizedText(manga.Attributes.Title);
        if (title is not null)
        {
            return title;
        }

        return ResolveLocalizedText(manga.Attributes.AltTitles.SelectMany(alt => alt).GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.First().Value))
            ?? manga.Id;
    }

    private static string? ResolveLocalizedText(IReadOnlyDictionary<string, string> localized)
    {
        if (localized.Count == 0)
        {
            return null;
        }

        if (localized.TryGetValue("en", out var value))
        {
            return value;
        }

        return localized.Values.FirstOrDefault();
    }

    private static string? CoverUrl(MangaDexManga manga, string coversBaseAddress)
    {
        var fileName = manga.Relationships
            .FirstOrDefault(relationship => relationship.Type == "cover_art")
            ?.Attributes
            ?.FileName;

        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : $"{coversBaseAddress.TrimEnd('/')}/covers/{manga.Id}/{fileName}.512.jpg";
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
