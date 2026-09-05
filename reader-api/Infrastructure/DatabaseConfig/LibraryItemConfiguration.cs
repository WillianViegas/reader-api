using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reader.Api.Domain.Entities;

namespace Reader.Api.Infrastructure.DatabaseConfig;

public sealed class LibraryItemConfiguration : IEntityTypeConfiguration<LibraryItem>
{
    public void Configure(EntityTypeBuilder<LibraryItem> builder)
    {
        builder.ToTable("LibraryItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedNever();

        builder.Property(item => item.UserId)
            .IsRequired();

        builder.HasIndex(item => item.UserId);

        builder.Property(item => item.IsFavorite)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.Property(item => item.UpdatedAt)
            .IsRequired();

        builder.OwnsOne(item => item.Manga, manga =>
        {
            manga.Property(reference => reference.Title)
                .HasColumnName("MangaTitle")
                .HasMaxLength(500)
                .IsRequired();

            manga.Property(reference => reference.CoverUrl)
                .HasColumnName("MangaCoverUrl")
                .HasMaxLength(2048);

            manga.Property(reference => reference.OriginalLanguage)
                .HasColumnName("MangaOriginalLanguage")
                .HasMaxLength(50);

            manga.Property(reference => reference.SnapshotUpdatedAt)
                .HasColumnName("MangaSnapshotUpdatedAt")
                .IsRequired();

            manga.OwnsOne(reference => reference.Id, id =>
            {
                id.Property(resource => resource.Provider)
                    .HasColumnName("MangaProvider")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                id.Property(resource => resource.Value)
                    .HasColumnName("MangaExternalId")
                    .HasMaxLength(200)
                    .IsRequired();
            });
        });

        builder.HasMany(item => item.ReadingProgresses)
            .WithOne()
            .HasForeignKey("LibraryItemId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(item => item.ReadingProgresses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
