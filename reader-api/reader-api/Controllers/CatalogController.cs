using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Enums;

namespace reader_api.Controllers;

[ApiController]
// [Authorize]
[Route("api/catalog")]
public sealed class CatalogController(
    SearchCatalogHandler searchCatalog,
    GetMangaDetailsHandler getMangaDetails,
    GetMangaChaptersHandler getMangaChapters,
    GetChapterPagesHandler getChapterPages) : ControllerBase
{
    [HttpGet("manga")]
    [ProducesResponseType<PagedResultDto<MangaSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResultDto<MangaSummaryDto>>> Search(
        [FromQuery] string? title,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await searchCatalog.HandleAsync(new SearchCatalogQuery(title, page, pageSize), cancellationToken));

    [HttpGet("manga/{mangaId}")]
    [ProducesResponseType<MangaDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MangaDetailsDto>> GetDetails(string mangaId, CancellationToken cancellationToken) =>
        await getMangaDetails.HandleAsync(new GetMangaDetailsQuery(ExternalCatalogProvider.MangaDex, mangaId), cancellationToken) is { } details
            ? Ok(details)
            : NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "The manga was not found in the external catalog." });

    [HttpGet("manga/{mangaId}/chapters")]
    [ProducesResponseType<PagedResultDto<ChapterSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResultDto<ChapterSummaryDto>>> GetChapters(
        string mangaId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getMangaChapters.HandleAsync(new GetMangaChaptersQuery(ExternalCatalogProvider.MangaDex, mangaId, page, pageSize), cancellationToken));

    [HttpGet("chapters/{chapterId}/pages")]
    [ProducesResponseType<ChapterPagesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChapterPagesDto>> GetPages(string chapterId, CancellationToken cancellationToken) =>
        await getChapterPages.HandleAsync(new GetChapterPagesQuery(ExternalCatalogProvider.MangaDex, chapterId), cancellationToken) is { } pages
            ? Ok(pages)
            : NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "The chapter pages are not available in the external catalog." });
}
