using Reader.Api.Application.Dtos;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;

namespace Reader.Api.Application.UseCases;

internal static class ApplicationMapper
{
    public static ExternalResourceId ToExternalId(ExternalCatalogProvider provider, string value) => new(provider, value);

    public static MangaReference ToDomain(MangaReferenceDto manga, DateTimeOffset snapshotUpdatedAt) => new(
        ToExternalId(manga.Provider, manga.ExternalId), manga.Title, manga.CoverUrl, manga.OriginalLanguage, snapshotUpdatedAt);

    public static ChapterReference ToDomain(ChapterReferenceDto chapter) => new(
        ToExternalId(chapter.Provider, chapter.ExternalId), chapter.Language, chapter.Title, chapter.Volume, chapter.Number);

    public static UserProfileDto ToDto(User user) => new(user.Id, user.ExternalSubject, user.DisplayName, user.CreatedAt, user.UpdatedAt);

    public static MangaReferenceDto ToDto(MangaReference manga) => new(manga.Id.Provider, manga.Id.Value, manga.Title, manga.CoverUrl, manga.OriginalLanguage);

    public static ChapterReferenceDto ToDto(ChapterReference chapter) => new(chapter.Id.Provider, chapter.Id.Value, chapter.Language, chapter.Title, chapter.Volume, chapter.Number);

    public static ReadingProgressDto ToDto(ReadingProgress progress) => new(progress.Id, ToDto(progress.Chapter), progress.CurrentPage, progress.PageCount, progress.LastReadAt, progress.CompletedAt);

    public static LibraryItemDto ToDto(LibraryItem item) => new(
        item.Id,
        ToDto(item.Manga),
        item.IsFavorite,
        item.CreatedAt,
        item.UpdatedAt,
        item.ReadingProgresses.Select(ToDto).ToList());
}