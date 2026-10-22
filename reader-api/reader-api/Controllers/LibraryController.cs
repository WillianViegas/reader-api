using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
    AddPdfToLibraryHandler addPdf,
    OpenLibraryPdfHandler openPdf,
    SetMangaFavoriteHandler setFavorite,
    GetContinueReadingHandler getContinueReading,
    GetReadingProgressHandler getReadingProgress,
    RegisterReadingProgressHandler registerProgress,
    CompleteChapterHandler completeChapter) : ControllerBase
{
    private const long MaxPdfUploadSizeBytes = 100 * 1024 * 1024;
    private const long MaxCoverSizeBytes = 2 * 1024 * 1024;
    private const long MaxPdfRequestSizeBytes = MaxPdfUploadSizeBytes + MaxCoverSizeBytes + 1024 * 1024;

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

    [HttpPost("pdfs")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxPdfRequestSizeBytes)]
    [ProducesResponseType<LibraryItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemDto>> AddPdf(
        [FromForm] AddPdfToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File.Length is 0 or > MaxPdfUploadSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid PDF size",
                Detail = "The PDF must be larger than 0 bytes and no larger than 100 MB."
            });
        }

        await using var content = request.File.OpenReadStream();
        var signature = new byte[5];
        var signatureLength = 0;
        while (signatureLength < signature.Length)
        {
            var bytesRead = await content.ReadAsync(signature.AsMemory(signatureLength), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            signatureLength += bytesRead;
        }

        if (signatureLength != signature.Length || !signature.AsSpan().SequenceEqual("%PDF-"u8))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid PDF file",
                Detail = "The uploaded file does not have a valid PDF signature."
            });
        }

        content.Position = 0;

        Stream? coverContent = null;
        if (request.Cover is not null)
        {
            if (request.Cover.Length is 0 or > MaxCoverSizeBytes)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid cover size",
                    Detail = "The cover must be larger than 0 bytes and no larger than 2 MB."
                });
            }

            coverContent = request.Cover.OpenReadStream();
        }

        await using var coverScope = coverContent;
        if (coverContent is not null)
        {
            var header = new byte[3];
            var headerLength = await coverContent.ReadAsync(header, cancellationToken);
            if (headerLength != header.Length || !header.AsSpan().SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid cover image",
                    Detail = "The cover must be a JPEG image."
                });
            }

            coverContent.Position = 0;
        }

        var item = await addPdf.HandleAsync(
            new AddPdfToLibraryCommand(currentUser.UserId, request.Title, content, coverContent),
            cancellationToken);

        logger.LogInformation(
            "Added PDF {PdfId} to the library for user {UserId}",
            item.Manga.ExternalId,
            currentUser.UserId);

        return CreatedAtAction(nameof(GetPdf), new { pdfId = item.Manga.ExternalId }, item);
    }

    [HttpGet("pdfs/{pdfId:guid}/cover")]
    [Produces("image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdfCover(Guid pdfId, CancellationToken cancellationToken)
    {
        var content = await openPdf.HandleCoverAsync(currentUser.UserId, pdfId, cancellationToken);
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(content, "image/jpeg");
    }

    [HttpGet("pdfs/{pdfId:guid}")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(Guid pdfId, CancellationToken cancellationToken)
    {
        var content = await openPdf.HandleAsync(currentUser.UserId, pdfId, cancellationToken);
        Response.Headers.ContentDisposition = "inline";
        return File(content, "application/pdf");
    }

    [HttpDelete("pdfs/{pdfId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePdf(Guid pdfId, CancellationToken cancellationToken)
    {
        await removeManga.HandleAsync(
            new RemoveMangaFromLibraryCommand(currentUser.UserId, ExternalCatalogProvider.UploadedPdf, pdfId.ToString("D")),
            cancellationToken);
        return NoContent();
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

    [HttpGet("pdfs/{pdfId:guid}/progress")]
    [ProducesResponseType<IReadOnlyList<ReadingProgressDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReadingProgressDto>>> GetPdfProgress(Guid pdfId, CancellationToken cancellationToken)
    {
        var progress = await getReadingProgress.HandleAsync(
            new GetReadingProgressQuery(ExternalCatalogProvider.UploadedPdf, pdfId.ToString()),
            cancellationToken);
        return Ok(progress);
    }

    [HttpPut("pdfs/{pdfId:guid}/progress")]
    [ProducesResponseType<ReadingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingProgressDto>> RegisterPdfProgress(Guid pdfId, RegisterPdfProgressRequest request, CancellationToken cancellationToken)
    {
        var id = pdfId.ToString();
        var chapter = new ChapterReferenceDto(ExternalCatalogProvider.UploadedPdf, id, "pt-br", null, null, null);
        var progress = await registerProgress.HandleAsync(
            new RegisterReadingProgressCommand(currentUser.UserId, ExternalCatalogProvider.UploadedPdf, id, chapter, request.CurrentPage, request.PageCount),
            cancellationToken);
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

/// <summary>Metadata and file for importing a PDF into the authenticated user's library.</summary>
public sealed record AddPdfToLibraryRequest
{
    /// <summary>Title displayed for the imported PDF.</summary>
    [Required, StringLength(500)]
    public required string Title { get; init; }

    /// <summary>PDF file to import; files are limited to 100 MB.</summary>
    [Required]
    public required IFormFile File { get; init; }

    /// <summary>Optional JPEG cover (first page) of up to 2 MB.</summary>
    public IFormFile? Cover { get; init; }
}

public sealed record SetFavoriteRequest(bool IsFavorite);

/// <summary>Reading position inside an imported PDF.</summary>
public sealed record RegisterPdfProgressRequest([Range(1, int.MaxValue)] int CurrentPage, [Range(1, int.MaxValue)] int PageCount);

public sealed record RegisterProgressRequest(ChapterReferenceDto Chapter, int CurrentPage, int? PageCount);
