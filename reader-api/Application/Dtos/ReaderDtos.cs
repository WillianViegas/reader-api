using Reader.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reader.Api.Application.Dtos;

public sealed record UserProfileDto(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
	[JsonIgnore]
	public string ExternalSubject => Email;
}

public sealed record MangaReferenceDto(ExternalCatalogProvider Provider, string ExternalId, string Title, string? CoverUrl, string? OriginalLanguage);

public sealed record MangaSummaryDto(ExternalCatalogProvider Provider, string Id, string Title, string? CoverUrl, string? OriginalLanguage);

public sealed record CatalogTagDto(string Id, string Name, string Group);

/// <summary>Request to save a translation to the current user's vocabulary.</summary>
public sealed record SaveVocabularyTranslationRequest
{
	[Required, StringLength(500)]
	public required string OriginalText { get; init; }

	[Required, StringLength(2000)]
	public required string TranslatedText { get; init; }

	[Required, RegularExpression("^(en|it|pt-BR)$")]
	public required string SourceLanguage { get; init; }

	[Required, RegularExpression("^(en|it|pt-BR)$")]
	public required string TargetLanguage { get; init; }

	[StringLength(500)]
	public string? ContextText { get; init; }

	[StringLength(200)]
	public string? MangaExternalId { get; init; }

	[StringLength(500)]
	public string? MangaTitle { get; init; }

	[StringLength(200)]
	public string? ChapterExternalId { get; init; }

	[StringLength(500)]
	public string? ChapterTitle { get; init; }

	[Range(1, int.MaxValue)]
	public int? PageNumber { get; init; }
}

/// <summary>A saved translation owned by the authenticated user.</summary>
public sealed record SavedVocabularyTranslationDto(
	Guid Id,
	string OriginalText,
	string TranslatedText,
	string SourceLanguage,
	string TargetLanguage,
	string? ContextText,
	string? MangaExternalId,
	string? MangaTitle,
	string? ChapterExternalId,
	string? ChapterTitle,
	int? PageNumber,
	DateTimeOffset SavedAt);

public sealed record MangaDetailsDto(ExternalCatalogProvider Provider, string Id, string Title, string? CoverUrl, string? OriginalLanguage, string? Description);

public sealed record ChapterReferenceDto(ExternalCatalogProvider Provider, string ExternalId, string Language, string? Title, string? Volume, string? Number);

public sealed record ChapterSummaryDto(ExternalCatalogProvider Provider, string Id, string Language, string? Title, string? Volume, string? Number, DateTimeOffset? PublishedAt);

public sealed record ChapterPagesDto(ExternalCatalogProvider Provider, string ChapterId, IReadOnlyList<string> PageUrls);

public sealed record ChapterPageImageDto(byte[] Content, string ContentType);

public sealed record ReadingProgressDto(Guid Id, ChapterReferenceDto Chapter, int CurrentPage, int? PageCount, DateTimeOffset LastReadAt, DateTimeOffset? CompletedAt);

public sealed record LibraryItemDto(Guid Id, MangaReferenceDto Manga, bool IsFavorite, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<ReadingProgressDto> Progresses);

public sealed record ContinueReadingDto(Guid LibraryItemId, MangaReferenceDto Manga, ReadingProgressDto Progress);

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ApplicationErrorDto(string Code, string Message);