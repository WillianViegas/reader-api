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
    ILogger<LibraryController> logger,
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
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Getting library for user {UserId}, favoriteOnly {FavoriteOnly}, page {Page}, pageSize {PageSize}",
            currentUser.UserId,
            favoriteOnly,
            page,
            pageSize);

        var result = await getLibrary.HandleAsync(new GetLibraryQuery(favoriteOnly, page, pageSize), cancellationToken);
        logger.LogInformation("Found {LibraryItemCount} library items for user {UserId}", result.TotalCount, currentUser.UserId);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<LibraryItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LibraryItemDto>> Add(AddLibraryItemRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Adding manga {MangaProvider}/{MangaExternalId} to library for user {UserId}",
            request.Manga.Provider,
            request.Manga.ExternalId,
            currentUser.UserId);

        var item = await addManga.HandleAsync(
            new AddMangaToLibraryCommand(currentUser.UserId, request.Manga, request.IsFavorite),
            cancellationToken);

        logger.LogInformation(
            "Added manga {MangaProvider}/{MangaExternalId} to library as item {LibraryItemId} for user {UserId}",
            item.Manga.Provider,
            item.Manga.ExternalId,
            item.Id,
            currentUser.UserId);

        return CreatedAtAction(nameof(GetProgress), new { mangaId = item.Manga.ExternalId }, item);
    }

    [HttpDelete("{mangaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string mangaId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing manga {MangaId} from library for user {UserId}", mangaId, currentUser.UserId);
        await removeManga.HandleAsync(
            new RemoveMangaFromLibraryCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId),
            cancellationToken);

        logger.LogInformation("Removed manga {MangaId} from library for user {UserId}", mangaId, currentUser.UserId);
        return NoContent();
    }

    [HttpPut("{mangaId}/favorite")]
    [ProducesResponseType<LibraryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LibraryItemDto>> SetFavorite(string mangaId, SetFavoriteRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting favorite {IsFavorite} for manga {MangaId} and user {UserId}", request.IsFavorite, mangaId, currentUser.UserId);
        var item = await setFavorite.HandleAsync(
            new SetMangaFavoriteCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, request.IsFavorite),
            cancellationToken);
        logger.LogInformation("Set favorite {IsFavorite} for manga {MangaId} and user {UserId}", item.IsFavorite, mangaId, currentUser.UserId);
        return Ok(item);
    }

    [HttpGet("continue-reading")]
    [ProducesResponseType<ContinueReadingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContinueReadingDto>> ContinueReading(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting continue-reading item for user {UserId}", currentUser.UserId);
        var resume = await getContinueReading.HandleAsync(new GetContinueReadingQuery(), cancellationToken);
        if (resume is null)
        {
            logger.LogInformation("No continue-reading item found for user {UserId}", currentUser.UserId);
            return NotFound();
        }

        logger.LogInformation("Found continue-reading manga {MangaId} for user {UserId}", resume.Manga.ExternalId, currentUser.UserId);
        return Ok(resume);
    }

    [HttpGet("{mangaId}/progress")]
    [ProducesResponseType<IReadOnlyList<ReadingProgressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReadingProgressDto>>> GetProgress(string mangaId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting reading progress for manga {MangaId} and user {UserId}", mangaId, currentUser.UserId);
        var progress = await getReadingProgress.HandleAsync(new GetReadingProgressQuery(ExternalCatalogProvider.MangaDex, mangaId), cancellationToken);
        logger.LogInformation("Found {ProgressCount} progress entries for manga {MangaId} and user {UserId}", progress.Count, mangaId, currentUser.UserId);
        return Ok(progress);
    }

    [HttpPut("{mangaId}/progress")]
    [ProducesResponseType<ReadingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingProgressDto>> RegisterProgress(string mangaId, RegisterProgressRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Registering progress for manga {MangaId}, chapter {ChapterId}, user {UserId}, page {CurrentPage}/{PageCount}",
            mangaId,
            request.Chapter.ExternalId,
            currentUser.UserId,
            request.CurrentPage,
            request.PageCount);
        var progress = await registerProgress.HandleAsync(
            new RegisterReadingProgressCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, request.Chapter, request.CurrentPage, request.PageCount),
            cancellationToken);
        logger.LogInformation("Registered progress {ProgressId} for manga {MangaId} and user {UserId}", progress.Id, mangaId, currentUser.UserId);
        return Ok(progress);
    }

    [HttpPost("{mangaId}/chapters/{chapterId}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(string mangaId, string chapterId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Completing chapter {ChapterId} for manga {MangaId} and user {UserId}", chapterId, mangaId, currentUser.UserId);
        await completeChapter.HandleAsync(
            new CompleteChapterCommand(currentUser.UserId, ExternalCatalogProvider.MangaDex, mangaId, ExternalCatalogProvider.MangaDex, chapterId),
            cancellationToken);

        logger.LogInformation("Completed chapter {ChapterId} for manga {MangaId} and user {UserId}", chapterId, mangaId, currentUser.UserId);
        return NoContent();
    }
}

public sealed record AddLibraryItemRequest(MangaReferenceDto Manga, bool IsFavorite = false);

public sealed record SetFavoriteRequest(bool IsFavorite);

public sealed record RegisterProgressRequest(ChapterReferenceDto Chapter, int CurrentPage, int? PageCount);
