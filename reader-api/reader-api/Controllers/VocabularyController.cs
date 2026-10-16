using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;

namespace reader_api.Controllers;

[ApiController]
[Authorize]
[Route("api/vocabulary")]
public sealed class VocabularyController(
    ICurrentUser currentUser,
    SaveVocabularyTranslationHandler saveTranslation,
    GetUserVocabularyHandler getVocabulary,
    DeleteVocabularyTranslationHandler deleteTranslation) : ControllerBase
{
    /// <summary>Lists saved translations belonging to the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<SavedVocabularyTranslationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<SavedVocabularyTranslationDto>>> Get(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await getVocabulary.HandleAsync(currentUser.UserId, new GetUserVocabularyQuery(page, pageSize), cancellationToken));

    /// <summary>Saves a translation for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType<SavedVocabularyTranslationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavedVocabularyTranslationDto>> Save(
        SaveVocabularyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var saved = await saveTranslation.HandleAsync(
            new SaveVocabularyTranslationCommand(
                currentUser.UserId,
                request.OriginalText,
                request.TranslatedText,
                request.SourceLanguage,
                request.TargetLanguage,
                request.ContextText,
                request.MangaExternalId,
                request.MangaTitle,
                request.ChapterExternalId,
                request.ChapterTitle,
                request.PageNumber),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    /// <summary>Gets one saved translation owned by the authenticated user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SavedVocabularyTranslationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedVocabularyTranslationDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        await getVocabulary.GetByIdAsync(currentUser.UserId, id, cancellationToken) is { } saved
            ? Ok(saved)
            : NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "The saved translation was not found." });

    /// <summary>Deletes one saved translation owned by the authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteTranslation.HandleAsync(new DeleteVocabularyTranslationCommand(currentUser.UserId, id), cancellationToken);
        return NoContent();
    }
}