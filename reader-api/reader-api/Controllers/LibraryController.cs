using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;

namespace reader_api.Controllers;

[ApiController]
[Authorize]
[Route("api/library")]
public sealed class LibraryController(
    ICurrentUser currentUser,
    GetLibraryHandler getLibrary,
    AddMangaToLibraryHandler addManga,
    RemoveMangaFromLibraryHandler removeManga,
    SetMangaFavoriteHandler setFavorite,
    GetContinueReadingHandler getContinueReading,
    GetReadingProgressHandler getReadingProgress,
    RegisterReadingProgressHandler registerProgress,
    CompleteChapterHandler completeChapter) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDto<LibraryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<LibraryItemDto>>> Get(
        [FromQuery] bool favoriteOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getLibrary.HandleAsync(new GetLibraryQuery(favoriteOnly, page, pageSize), cancellationToken));

    [HttpPost]
    [ProducesResponseType<LibraryItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LibraryItemDto>> Add(AddLibraryItemRequest request, CancellationToken cancellationToken)
    {
        var item = await addManga.HandleAsync(
            new AddMangaToLibraryCommand(currentUser.UserId, request.Manga, request.IsFavorite),
            cancellationToken);

        return CreatedAtAction(nameof(GetProgress), new { mangaId = item.Manga.ExternalId }, item);
    }

    [HttpDelete("{mangaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string mangaId, CancellationToken cancellationToken)
    {
        await removeManga.HandleAsync(
            new RemoveMangaFromLibraryCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId),
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{mangaId}/favorite")]
    [ProducesResponseType<LibraryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LibraryItemDto>> SetFavorite(string mangaId, SetFavoriteRequest request, CancellationToken cancellationToken) =>
        Ok(await setFavorite.HandleAsync(
            new SetMangaFavoriteCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, request.IsFavorite),
            cancellationToken));

    [HttpGet("continue-reading")]
    [ProducesResponseType<ContinueReadingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContinueReadingDto>> ContinueReading(CancellationToken cancellationToken) =>
        await getContinueReading.HandleAsync(new GetContinueReadingQuery(), cancellationToken) is { } resume
            ? Ok(resume)
            : NotFound();

    [HttpGet("{mangaId}/progress")]
    [ProducesResponseType<IReadOnlyList<ReadingProgressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReadingProgressDto>>> GetProgress(string mangaId, CancellationToken cancellationToken) =>
        Ok(await getReadingProgress.HandleAsync(new GetReadingProgressQuery(ExternalCatalogProvider.MangaDex, mangaId), cancellationToken));

    [HttpPut("{mangaId}/progress")]
    [ProducesResponseType<ReadingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingProgressDto>> RegisterProgress(string mangaId, RegisterProgressRequest request, CancellationToken cancellationToken) =>
        Ok(await registerProgress.HandleAsync(
            new RegisterReadingProgressCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, request.Chapter, request.CurrentPage, request.PageCount),
            cancellationToken));

    [HttpPost("{mangaId}/chapters/{chapterId}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(string mangaId, string chapterId, CancellationToken cancellationToken)
    {
        await completeChapter.HandleAsync(
            new CompleteChapterCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, ExternalCatalogProvider.MangaDex, chapterId),
            cancellationToken);

        return NoContent();
    }
}

public sealed record AddLibraryItemRequest(MangaReferenceDto Manga, bool IsFavorite = false);

public sealed record SetFavoriteRequest(bool IsFavorite);

public sealed record RegisterProgressRequest(ChapterReferenceDto Chapter, int CurrentPage, int? PageCount);
