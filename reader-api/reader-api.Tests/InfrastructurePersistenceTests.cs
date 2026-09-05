using Microsoft.EntityFrameworkCore;
using Reader.Api.Domain.Entities;
using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;
using Reader.Api.Infrastructure.DatabaseConfig;
using Reader.Api.Infrastructure.Repositories;

namespace reader_api.Tests;

public class InfrastructurePersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_WhenMangaIsDuplicatedForTheSameUser_ViolatesTheUniqueIndex()
    {
        var databasePath = NewDatabasePath();
        try
        {
            await using var context = await CreateContextAsync(databasePath);
            var repository = new EfLibraryRepository(context);
            var userId = Guid.NewGuid();

            await repository.AddAsync(CreateItem(userId));
            await context.SaveChangesAsync();
            await repository.AddAsync(CreateItem(userId));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task SaveChanges_AllowsTheSameMangaForDifferentUsers()
    {
        var databasePath = NewDatabasePath();
        try
        {
            await using var context = await CreateContextAsync(databasePath);
            var repository = new EfLibraryRepository(context);

            await repository.AddAsync(CreateItem(Guid.NewGuid()));
            await repository.AddAsync(CreateItem(Guid.NewGuid()));
            await context.SaveChangesAsync();

            Assert.Equal(2, await context.LibraryItems.CountAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Remove_WhenTheItemHasProgress_CascadeDeletesItsReadingProgresses()
    {
        var databasePath = NewDatabasePath();
        try
        {
            var userId = Guid.NewGuid();

            await using (var writeContext = await CreateContextAsync(databasePath))
            {
                var item = CreateItem(userId);
                item.RegisterProgress(CreateChapter("chapter-1"), 3, 10, Now);
                await writeContext.LibraryItems.AddAsync(item);
                await writeContext.SaveChangesAsync();
            }

            await using (var removeContext = await CreateContextAsync(databasePath, migrate: false))
            {
                var repository = new EfLibraryRepository(removeContext);
                var item = await repository.GetByUserAndMangaAsync(userId, MangaId());

                Assert.NotNull(item);
                Assert.Single(item.ReadingProgresses);

                await repository.RemoveAsync(item);
                await removeContext.SaveChangesAsync();
            }

            await using var readContext = await CreateContextAsync(databasePath, migrate: false);
            Assert.Empty(await readContext.LibraryItems.ToListAsync());
            Assert.Empty(await readContext.Set<ReadingProgress>().ToListAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetByUserAndManga_RoundTripsTheOwnedValueObjects()
    {
        var databasePath = NewDatabasePath();
        try
        {
            var userId = Guid.NewGuid();

            await using (var writeContext = await CreateContextAsync(databasePath))
            {
                var item = CreateItem(userId);
                item.RegisterProgress(CreateChapter("chapter-1"), 3, 10, Now);
                await writeContext.LibraryItems.AddAsync(item);
                await writeContext.SaveChangesAsync();
            }

            await using var readContext = await CreateContextAsync(databasePath, migrate: false);
            var loadedItem = await new EfLibraryRepository(readContext).GetByUserAndMangaAsync(userId, MangaId());

            Assert.NotNull(loadedItem);
            Assert.Equal(ExternalCatalogProvider.MangaDex, loadedItem.Manga.Id.Provider);
            Assert.Equal("manga-1", loadedItem.Manga.Id.Value);
            Assert.Equal("Manga One", loadedItem.Manga.Title);
            Assert.Equal("pt-br", loadedItem.Manga.OriginalLanguage);
            Assert.Equal(Now, loadedItem.Manga.SnapshotUpdatedAt);

            var progress = Assert.Single(loadedItem.ReadingProgresses);
            Assert.Equal("chapter-1", progress.Chapter.Id.Value);
            Assert.Equal("pt-br", progress.Chapter.Language);
            Assert.Equal("1", progress.Chapter.Volume);
            Assert.Equal(3, progress.CurrentPage);
            Assert.Equal(10, progress.PageCount);
            Assert.Equal(Now, progress.LastReadAt);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static async Task<ReaderDbContext> CreateContextAsync(string databasePath, bool migrate = true)
    {
        var options = new DbContextOptionsBuilder<ReaderDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=false")
            .Options;
        var context = new ReaderDbContext(options);

        if (migrate)
        {
            await context.Database.EnsureCreatedAsync();
            foreach (var statement in SupplementalIndexes)
            {
                await context.Database.ExecuteSqlRawAsync(statement);
            }
        }

        return context;
    }

    // Mirrors the composite indexes added to the InitialCreate migration, which EF Core
    // cannot express in the model because they span owner and owned-type columns.
    private static readonly string[] SupplementalIndexes =
    [
        """CREATE UNIQUE INDEX "IX_LibraryItems_UserId_MangaProvider_MangaExternalId" ON "LibraryItems" ("UserId", "MangaProvider", "MangaExternalId");""",
        """CREATE INDEX "IX_LibraryItems_MangaProvider_MangaExternalId" ON "LibraryItems" ("MangaProvider", "MangaExternalId");""",
        """CREATE INDEX "IX_ReadingProgresses_ChapterProvider_ChapterExternalId" ON "ReadingProgresses" ("ChapterProvider", "ChapterExternalId");"""
    ];

    private static ExternalResourceId MangaId() => new(ExternalCatalogProvider.MangaDex, "manga-1");

    private static LibraryItem CreateItem(Guid userId) => new(
        Guid.NewGuid(),
        userId,
        new MangaReference(MangaId(), "Manga One", "https://example.test/cover.jpg", "pt-br", Now),
        false,
        Now);

    private static ChapterReference CreateChapter(string id) => new(
        new ExternalResourceId(ExternalCatalogProvider.MangaDex, id),
        "pt-br",
        title: "Chapter",
        volume: "1",
        number: "3");

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"reader-persistence-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of the temporary test database.
        }
    }
}
