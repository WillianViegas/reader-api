using Reader.Api.Domain.Enums;

namespace Reader.Api.Application.Dtos;

public sealed record UserProfileDto(Guid Id, string ExternalSubject, string DisplayName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record MangaReferenceDto(ExternalCatalogProvider Provider, string ExternalId, string Title, string? CoverUrl, string? OriginalLanguage);

public sealed record MangaSummaryDto(ExternalCatalogProvider Provider, string Id, string Title, string? CoverUrl, string? OriginalLanguage);

public sealed record MangaDetailsDto(ExternalCatalogProvider Provider, string Id, string Title, string? CoverUrl, string? OriginalLanguage, string? Description);

public sealed record ChapterReferenceDto(ExternalCatalogProvider Provider, string ExternalId, string Language, string? Title, string? Volume, string? Number);

public sealed record ChapterSummaryDto(ExternalCatalogProvider Provider, string Id, string Language, string? Title, string? Volume, string? Number, DateTimeOffset? PublishedAt);

public sealed record ChapterPagesDto(ExternalCatalogProvider Provider, string ChapterId, IReadOnlyList<string> PageUrls);

public sealed record ReadingProgressDto(Guid Id, ChapterReferenceDto Chapter, int CurrentPage, int? PageCount, DateTimeOffset LastReadAt, DateTimeOffset? CompletedAt);

public sealed record LibraryItemDto(Guid Id, MangaReferenceDto Manga, bool IsFavorite, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<ReadingProgressDto> Progresses);

public sealed record ContinueReadingDto(Guid LibraryItemId, MangaReferenceDto Manga, ReadingProgressDto Progress);

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ApplicationErrorDto(string Code, string Message);