using Reader.Api.Application.Dtos;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.Exceptions;
using Reader.Api.Domain.ValueObjects;

namespace reader_api.Tests;

public class ApplicationUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthenticateUser_CreatesUserWhenExternalSubjectIsNew()
    {
        var users = new InMemoryUserRepository();
        var unitOfWork = new CountingUnitOfWork();
        var handler = new AuthenticateUserHandler(users, unitOfWork, new FixedClock(Now));

        var result = await handler.HandleAsync(new AuthenticateUserCommand("firebase-123", "Reader"));

        Assert.Equal("firebase-123", result.ExternalSubject);
        Assert.Single(users.Users);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterUser_ReturnsExistingUserWithoutSaving()
    {
        var existing = new User(Guid.NewGuid(), "firebase-123", "Existing", Now);
        var users = new InMemoryUserRepository(existing);
        var unitOfWork = new CountingUnitOfWork();
        var handler = new RegisterUserHandler(users, unitOfWork, new FixedClock(Now));

        var result = await handler.HandleAsync(new RegisterUserCommand("firebase-123", "Ignored"));

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal("Existing", result.DisplayName);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddMangaToLibrary_RejectsDuplicateMangaForUser()
    {
        var library = new InMemoryLibraryRepository();
        var handler = new AddMangaToLibraryHandler(library, new CountingUnitOfWork(), new FixedClock(Now));
        var command = new AddMangaToLibraryCommand(Guid.NewGuid(), Manga());

        await handler.HandleAsync(command);

        await Assert.ThrowsAsync<LibraryItemAlreadyExistsException>(() => handler.HandleAsync(command));
    }

    [Fact]
    public async Task SetMangaFavorite_UpdatesTheOwnedLibraryItem()
    {
        var userId = Guid.NewGuid();
        var item = Item(userId);
        var library = new InMemoryLibraryRepository(item);
        var unitOfWork = new CountingUnitOfWork();
        var handler = new SetMangaFavoriteHandler(library, unitOfWork, new FixedClock(Now));

        var result = await handler.HandleAsync(new SetMangaFavoriteCommand(userId, ExternalCatalogProvider.MangaDex, "manga-1", true));

        Assert.True(result.IsFavorite);
        Assert.Equal(Now, result.UpdatedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemoveMangaFromLibrary_RemovesOnlyTheMatchedItem()
    {
        var userId = Guid.NewGuid();
        var library = new InMemoryLibraryRepository(Item(userId));
        var handler = new RemoveMangaFromLibraryHandler(library, new CountingUnitOfWork());

        await handler.HandleAsync(new RemoveMangaFromLibraryCommand(userId, ExternalCatalogProvider.MangaDex, "manga-1"));

        Assert.Empty(library.Items);
    }

    [Fact]
    public async Task RegisterReadingProgress_UsesClockAndSavesAggregate()
    {
        var userId = Guid.NewGuid();
        var library = new InMemoryLibraryRepository(Item(userId));
        var unitOfWork = new CountingUnitOfWork();
        var handler = new RegisterReadingProgressHandler(library, unitOfWork, new FixedClock(Now));

        var result = await handler.HandleAsync(new RegisterReadingProgressCommand(userId, ExternalCatalogProvider.MangaDex, "manga-1", Chapter("chapter-1"), 2, 12));

        Assert.Equal(Now, result.LastReadAt);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CompleteChapter_CompletesExistingProgress()
    {
        var userId = Guid.NewGuid();
        var item = Item(userId);
        item.RegisterProgress(DomainChapter("chapter-1"), 2, 12, Now.AddMinutes(-1));
        var unitOfWork = new CountingUnitOfWork();
        var handler = new CompleteChapterHandler(new InMemoryLibraryRepository(item), unitOfWork, new FixedClock(Now));

        await handler.HandleAsync(new CompleteChapterCommand(userId, ExternalCatalogProvider.MangaDex, "manga-1", ExternalCatalogProvider.MangaDex, "chapter-1"));

        Assert.True(item.ReadingProgresses.Single().IsCompleted);
        Assert.Equal(Now, item.ReadingProgresses.Single().CompletedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetLibrary_UsesTheAuthenticatedUserAndFavoriteFilter()
    {
        var userId = Guid.NewGuid();
        var favorite = Item(userId, isFavorite: true);
        var otherUserItem = Item(Guid.NewGuid());
        var handler = new GetLibraryHandler(new InMemoryLibraryRepository(favorite, otherUserItem), new TestCurrentUser(userId));

        var result = await handler.HandleAsync(new GetLibraryQuery(FavoriteOnly: true));

        Assert.Single(result.Items);
        Assert.Equal(favorite.Id, result.Items.Single().Id);
    }

    [Fact]
    public async Task GetContinueReading_PrefersTheMostRecentIncompleteProgress()
    {
        var userId = Guid.NewGuid();
        var completedItem = Item(userId, "completed");
        completedItem.RegisterProgress(DomainChapter("complete-chapter"), 10, 10, Now);
        var inProgressItem = Item(userId, "in-progress");
        inProgressItem.RegisterProgress(DomainChapter("reading-chapter"), 3, 10, Now.AddMinutes(-1));
        var handler = new GetContinueReadingHandler(new InMemoryLibraryRepository(completedItem, inProgressItem), new TestCurrentUser(userId));

        var result = await handler.HandleAsync(new GetContinueReadingQuery());

        Assert.NotNull(result);
        Assert.Equal("in-progress", result.Manga.ExternalId);
    }

    [Fact]
    public async Task GetUserProfile_UsesCurrentUserInsteadOfACallerSuppliedId()
    {
        var user = new User(Guid.NewGuid(), "firebase-123", "Reader", Now);
        var handler = new GetUserProfileHandler(new InMemoryUserRepository(user), new TestCurrentUser(user.Id));

        var result = await handler.HandleAsync(new GetUserProfileQuery());

        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task CatalogHandlers_DelegateToTheirProviderPorts()
    {
        var catalog = new FakeCatalogProvider();
        var chapters = new FakeChapterProvider();

        var search = await new SearchCatalogHandler(catalog).HandleAsync(new SearchCatalogQuery("Manga"));
        var details = await new GetMangaDetailsHandler(catalog).HandleAsync(new GetMangaDetailsQuery(ExternalCatalogProvider.MangaDex, "manga-1"));
        var chapterPage = await new GetMangaChaptersHandler(chapters).HandleAsync(new GetMangaChaptersQuery(ExternalCatalogProvider.MangaDex, "manga-1"));
        var pages = await new GetChapterPagesHandler(chapters).HandleAsync(new GetChapterPagesQuery(ExternalCatalogProvider.MangaDex, "chapter-1"));

        Assert.Single(search.Items);
        Assert.NotNull(details);
        Assert.Single(chapterPage.Items);
        Assert.NotNull(pages);
    }

    private static MangaReferenceDto Manga(string id = "manga-1") => new(ExternalCatalogProvider.MangaDex, id, "Manga", null, "pt-br");

    private static ChapterReferenceDto Chapter(string id) => new(ExternalCatalogProvider.MangaDex, id, "pt-br", null, null, null);

    private static ChapterReference DomainChapter(string id) => new(new ExternalResourceId(ExternalCatalogProvider.MangaDex, id), "pt-br");

    private static LibraryItem Item(Guid userId, string mangaId = "manga-1", bool isFavorite = false) => new(
        Guid.NewGuid(), userId, new MangaReference(new ExternalResourceId(ExternalCatalogProvider.MangaDex, mangaId), "Manga", null, "pt-br", Now), isFavorite, Now);

    private sealed class InMemoryUserRepository(params User[] users) : IUserRepository
    {
        public List<User> Users { get; } = [.. users];

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByExternalSubjectAsync(string externalSubject, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.ExternalSubject == externalSubject));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
    }

    private sealed class InMemoryLibraryRepository(params LibraryItem[] items) : ILibraryRepository
    {
        public List<LibraryItem> Items { get; } = [.. items];

        public Task AddAsync(LibraryItem item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LibraryItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LibraryItem>>(Items.Where(item => item.UserId == userId).ToList());

        public Task<LibraryItem?> GetByUserAndMangaAsync(Guid userId, ExternalResourceId mangaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId && item.Manga.Id == mangaId));

        public Task RemoveAsync(LibraryItem item, CancellationToken cancellationToken = default)
        {
            Items.Remove(item);
            return Task.CompletedTask;
        }

        public Task SaveAsync(LibraryItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FakeCatalogProvider : ICatalogProvider
    {
        public Task<MangaDetailsDto?> GetDetailsAsync(ExternalResourceId mangaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MangaDetailsDto?>(new MangaDetailsDto(mangaId.Provider, mangaId.Value, "Manga", null, "pt-br", null));

        public Task<PagedResultDto<MangaSummaryDto>> SearchAsync(SearchCatalogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResultDto<MangaSummaryDto>([new MangaSummaryDto(ExternalCatalogProvider.MangaDex, "manga-1", query.Title, null, "pt-br")], 1, 20, 1));
    }

    private sealed class FakeChapterProvider : IChapterProvider
    {
        public Task<PagedResultDto<ChapterSummaryDto>> GetChaptersAsync(GetMangaChaptersQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResultDto<ChapterSummaryDto>([new ChapterSummaryDto(query.Provider, "chapter-1", "pt-br", null, null, null, null)], 1, 20, 1));

        public Task<ChapterPagesDto?> GetPagesAsync(ExternalResourceId chapterId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ChapterPagesDto?>(new ChapterPagesDto(chapterId.Provider, chapterId.Value, ["https://example.test/1.jpg"]));
    }
}