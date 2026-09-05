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
    GetChapterPagesHandler getChapterPages,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly System.Text.RegularExpressions.Regex MangaIdPattern = new("^[0-9a-fA-F-]{36}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex CoverFileNamePattern = new("^[a-zA-Z0-9_-]+\\.(?:jpg|jpeg|png|webp|gif)(?:\\.(?:256|512))?\\.(?:jpg|jpeg|png|webp|gif)$|^[a-zA-Z0-9_-]+\\.(?:jpg|jpeg|png|webp|gif)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    [HttpGet("manga")]
    [ProducesResponseType<PagedResultDto<MangaSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResultDto<MangaSummaryDto>>> Search(
        [FromQuery] string? title,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await searchCatalog.HandleAsync(new SearchCatalogQuery(title, page, pageSize, category), cancellationToken));

    [HttpGet("manga/{mangaId}")]
    [ProducesResponseType<MangaDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MangaDetailsDto>> GetDetails(string mangaId, CancellationToken cancellationToken) =>
        await getMangaDetails.HandleAsync(new GetMangaDetailsQuery(ExternalCatalogProvider.MangaDex, mangaId), cancellationToken) is { } details
            ? Ok(details)
            : NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "The manga was not found in the external catalog." });

    [HttpGet("covers/{mangaId}/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCover(string mangaId, string fileName, CancellationToken cancellationToken)
    {
        if (!MangaIdPattern.IsMatch(mangaId) || !CoverFileNamePattern.IsMatch(fileName))
        {
            return NotFound();
        }

        using var response = await httpClientFactory.CreateClient("MangaDexCovers")
            .GetAsync($"covers/{mangaId}/{fileName}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(content, response.Content.Headers.ContentType?.ToString() ?? "image/jpeg");
    }

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
