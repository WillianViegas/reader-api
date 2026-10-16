using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Entities;

namespace Reader.Api.Application.UseCases;

public sealed record SaveVocabularyTranslationCommand(
    Guid UserId,
    string OriginalText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string? ContextText,
    string? MangaExternalId,
    string? MangaTitle,
    string? ChapterExternalId,
    string? ChapterTitle,
    int? PageNumber);

public sealed record GetUserVocabularyQuery(int Page = 1, int PageSize = 50);

public sealed record DeleteVocabularyTranslationCommand(Guid UserId, Guid TranslationId);

public sealed class SaveVocabularyTranslationHandler(
    ISavedTranslationRepository translations,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<SavedVocabularyTranslationDto> HandleAsync(
        SaveVocabularyTranslationCommand command,
        CancellationToken cancellationToken = default)
    {
        var translation = new SavedTranslation(
            Guid.NewGuid(),
            command.UserId,
            command.OriginalText,
            command.TranslatedText,
            command.SourceLanguage,
            command.TargetLanguage,
            clock.UtcNow,
            command.ContextText,
            command.MangaExternalId,
            command.MangaTitle,
            command.ChapterExternalId,
            command.ChapterTitle,
            command.PageNumber);

        await translations.AddAsync(translation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(translation);
    }

    internal static SavedVocabularyTranslationDto ToDto(SavedTranslation translation) => new(
        translation.Id,
        translation.OriginalText,
        translation.TranslatedText,
        translation.SourceLanguage,
        translation.TargetLanguage,
        translation.ContextText,
        translation.MangaExternalId,
        translation.MangaTitle,
        translation.ChapterExternalId,
        translation.ChapterTitle,
        translation.PageNumber,
        translation.SavedAt);
}

public sealed class GetUserVocabularyHandler(ISavedTranslationRepository translations)
{
    public async Task<PagedResultDto<SavedVocabularyTranslationDto>> HandleAsync(
        Guid userId,
        GetUserVocabularyQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page must be positive and page size must be between 1 and 100.");
        }

        var result = await translations.GetByUserAsync(userId, query.Page, query.PageSize, cancellationToken);
        return new PagedResultDto<SavedVocabularyTranslationDto>(
            result.Items.Select(SaveVocabularyTranslationHandler.ToDto).ToList(),
            query.Page,
            query.PageSize,
            result.TotalCount);
    }

    public async Task<SavedVocabularyTranslationDto?> GetByIdAsync(
        Guid userId,
        Guid translationId,
        CancellationToken cancellationToken = default)
    {
        var translation = await translations.GetByUserAndIdAsync(userId, translationId, cancellationToken);
        return translation is null ? null : SaveVocabularyTranslationHandler.ToDto(translation);
    }
}

public sealed class DeleteVocabularyTranslationHandler(
    ISavedTranslationRepository translations,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(DeleteVocabularyTranslationCommand command, CancellationToken cancellationToken = default)
    {
        var translation = await translations.GetByUserAndIdAsync(command.UserId, command.TranslationId, cancellationToken)
            ?? throw new KeyNotFoundException("The saved translation was not found.");

        await translations.RemoveAsync(translation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}