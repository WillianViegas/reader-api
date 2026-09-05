using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reader.Api.Domain.Entities;

namespace Reader.Api.Infrastructure.DatabaseConfig;

public sealed class ReadingProgressConfiguration : IEntityTypeConfiguration<ReadingProgress>
{
    public void Configure(EntityTypeBuilder<ReadingProgress> builder)
    {
        builder.ToTable("ReadingProgresses");

        builder.HasKey(progress => progress.Id);

        builder.Property(progress => progress.Id)
            .ValueGeneratedNever();

        builder.Property(progress => progress.CurrentPage)
            .IsRequired();

        builder.Property(progress => progress.PageCount);

        builder.Property(progress => progress.LastReadAt)
            .IsRequired();

        builder.Property(progress => progress.CompletedAt);

        builder.OwnsOne(progress => progress.Chapter, chapter =>
        {
            chapter.Property(reference => reference.Language)
                .HasColumnName("ChapterLanguage")
                .HasMaxLength(50)
                .IsRequired();

            chapter.Property(reference => reference.Title)
                .HasColumnName("ChapterTitle")
                .HasMaxLength(500);

            chapter.Property(reference => reference.Volume)
                .HasColumnName("ChapterVolume")
                .HasMaxLength(50);

            chapter.Property(reference => reference.Number)
                .HasColumnName("ChapterNumber")
                .HasMaxLength(50);

            chapter.OwnsOne(reference => reference.Id, id =>
            {
                id.Property(resource => resource.Provider)
                    .HasColumnName("ChapterProvider")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                id.Property(resource => resource.Value)
                    .HasColumnName("ChapterExternalId")
                    .HasMaxLength(200)
                    .IsRequired();
            });
        });
    }
}
