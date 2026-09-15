using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.Exceptions;
using Reader.Api.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Reader.Api.Application.UseCases;

public sealed record RegisterUserCommand(string ExternalSubject, string DisplayName);

public sealed record AuthenticateUserCommand(string ExternalSubject, string DisplayName);

public sealed record AddMangaToLibraryCommand(Guid UserId, MangaReferenceDto Manga, bool IsFavorite = false);

public sealed record RemoveMangaFromLibraryCommand(Guid UserId, ExternalCatalogProvider Provider, string MangaId);

public sealed record SetMangaFavoriteCommand(Guid UserId, ExternalCatalogProvider Provider, string MangaId, bool IsFavorite);

public sealed record RegisterReadingProgressCommand(Guid UserId, ExternalCatalogProvider MangaProvider, string MangaId, ChapterReferenceDto Chapter, int CurrentPage, int? PageCount);

public sealed record CompleteChapterCommand(Guid UserId, ExternalCatalogProvider MangaProvider, string MangaId, ExternalCatalogProvider ChapterProvider, string ChapterId);

public sealed class RegisterUserHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RegisterUserHandler>? logger = null)
{
    public async Task<UserProfileDto> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Registering user");
        var existing = await users.GetByExternalSubjectAsync(command.ExternalSubject, cancellationToken);
        if (existing is not null)
        {
            logger?.LogInformation("Registration returned existing user {UserId}", existing.Id);
            return ApplicationMapper.ToDto(existing);
        }

        var user = new User(Guid.NewGuid(), command.ExternalSubject, command.DisplayName, clock.UtcNow);
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Registered user {UserId}", user.Id);
        return ApplicationMapper.ToDto(user);
    }
}

public sealed class AuthenticateUserHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuthenticateUserHandler>? logger = null)
{
    public async Task<UserProfileDto> HandleAsync(AuthenticateUserCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Authenticating user");
        var existing = await users.GetByExternalSubjectAsync(command.ExternalSubject, cancellationToken);
        if (existing is not null)
        {
            logger?.LogInformation("Authenticated existing user {UserId}", existing.Id);
            return ApplicationMapper.ToDto(existing);
        }

        var user = new User(Guid.NewGuid(), command.ExternalSubject, command.DisplayName, clock.UtcNow);
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Authenticated and created user {UserId}", user.Id);
        return ApplicationMapper.ToDto(user);
    }
}

public sealed class AddMangaToLibraryHandler(
    ILibraryRepository library,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AddMangaToLibraryHandler>? logger = null)
{
    public async Task<LibraryItemDto> HandleAsync(AddMangaToLibraryCommand command, CancellationToken cancellationToken = default)
    {
        var mangaId = ApplicationMapper.ToExternalId(command.Manga.Provider, command.Manga.ExternalId);
        logger?.LogInformation(
            "Checking whether manga {MangaProvider}/{MangaExternalId} is already in library for user {UserId}",
            mangaId.Provider,
            mangaId.Value,
            command.UserId);

        if (await library.GetByUserAndMangaAsync(command.UserId, mangaId, cancellationToken) is not null)
        {
            logger?.LogWarning(
                "Manga {MangaProvider}/{MangaExternalId} is already in library for user {UserId}",
                mangaId.Provider,
                mangaId.Value,
                command.UserId);
            throw new LibraryItemAlreadyExistsException("The manga is already in the user's library.");
        }

        var now = clock.UtcNow;
        var item = new LibraryItem(Guid.NewGuid(), command.UserId, ApplicationMapper.ToDomain(command.Manga, now), command.IsFavorite, now);
        await library.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation(
            "Persisted library item {LibraryItemId} for manga {MangaProvider}/{MangaExternalId} and user {UserId}",
            item.Id,
            mangaId.Provider,
            mangaId.Value,
            command.UserId);
        return ApplicationMapper.ToDto(item);
    }
}

public sealed class RemoveMangaFromLibraryHandler(
    ILibraryRepository library,
    IUnitOfWork unitOfWork,
    ILogger<RemoveMangaFromLibraryHandler>? logger = null)
{
    public async Task HandleAsync(RemoveMangaFromLibraryCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Removing manga {MangaProvider}/{MangaId} for user {UserId}", command.Provider, command.MangaId, command.UserId);
        var item = await library.GetByUserAndMangaAsync(command.UserId, ApplicationMapper.ToExternalId(command.Provider, command.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        await library.RemoveAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Removed library item {LibraryItemId} for user {UserId}", item.Id, command.UserId);
    }
}

public sealed class SetMangaFavoriteHandler(
    ILibraryRepository library,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<SetMangaFavoriteHandler>? logger = null)
{
    public async Task<LibraryItemDto> HandleAsync(SetMangaFavoriteCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Setting favorite {IsFavorite} for manga {MangaProvider}/{MangaId} and user {UserId}", command.IsFavorite, command.Provider, command.MangaId, command.UserId);
        var item = await library.GetByUserAndMangaAsync(command.UserId, ApplicationMapper.ToExternalId(command.Provider, command.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        item.SetFavorite(command.IsFavorite, clock.UtcNow);
        await library.SaveAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Set favorite {IsFavorite} for library item {LibraryItemId}", item.IsFavorite, item.Id);
        return ApplicationMapper.ToDto(item);
    }
}

public sealed class RegisterReadingProgressHandler(
    ILibraryRepository library,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RegisterReadingProgressHandler>? logger = null)
{
    public async Task<ReadingProgressDto> HandleAsync(RegisterReadingProgressCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Registering progress for manga {MangaProvider}/{MangaId}, chapter {ChapterProvider}/{ChapterId}, user {UserId}, page {CurrentPage}/{PageCount}", command.MangaProvider, command.MangaId, command.Chapter.Provider, command.Chapter.ExternalId, command.UserId, command.CurrentPage, command.PageCount);
        var item = await library.GetByUserAndMangaAsync(command.UserId, ApplicationMapper.ToExternalId(command.MangaProvider, command.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        var progress = item.RegisterProgress(ApplicationMapper.ToDomain(command.Chapter), command.CurrentPage, command.PageCount, clock.UtcNow);
        await library.SaveAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Registered progress {ProgressId} for library item {LibraryItemId}", progress.Id, item.Id);
        return ApplicationMapper.ToDto(progress);
    }
}

public sealed class CompleteChapterHandler(
    ILibraryRepository library,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CompleteChapterHandler>? logger = null)
{
    public async Task HandleAsync(CompleteChapterCommand command, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Completing chapter {ChapterProvider}/{ChapterId} for manga {MangaProvider}/{MangaId} and user {UserId}", command.ChapterProvider, command.ChapterId, command.MangaProvider, command.MangaId, command.UserId);
        var item = await library.GetByUserAndMangaAsync(command.UserId, ApplicationMapper.ToExternalId(command.MangaProvider, command.MangaId), cancellationToken)
            ?? throw new KeyNotFoundException("The manga was not found in the user's library.");

        item.MarkChapterCompleted(ApplicationMapper.ToExternalId(command.ChapterProvider, command.ChapterId), clock.UtcNow);
        await library.SaveAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Completed chapter {ChapterId} for library item {LibraryItemId}", command.ChapterId, item.Id);
    }
}