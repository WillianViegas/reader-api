using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reader.Api.Domain.Entities;

namespace Reader.Api.Infrastructure.DatabaseConfig;

public sealed class SavedTranslationConfiguration : IEntityTypeConfiguration<SavedTranslation>
{
    public void Configure(EntityTypeBuilder<SavedTranslation> builder)
    {
        builder.ToTable("SavedTranslations");
        builder.HasKey(translation => translation.Id);

        builder.Property(translation => translation.Id).ValueGeneratedNever();
        builder.Property(translation => translation.UserId).IsRequired();
        builder.Property(translation => translation.OriginalText).HasMaxLength(500).IsRequired();
        builder.Property(translation => translation.TranslatedText).HasMaxLength(2000).IsRequired();
        builder.Property(translation => translation.SourceLanguage).HasMaxLength(10).IsRequired();
        builder.Property(translation => translation.TargetLanguage).HasMaxLength(10).IsRequired();
        builder.Property(translation => translation.ContextText).HasMaxLength(500);
        builder.Property(translation => translation.MangaExternalId).HasMaxLength(200);
        builder.Property(translation => translation.MangaTitle).HasMaxLength(500);
        builder.Property(translation => translation.ChapterExternalId).HasMaxLength(200);
        builder.Property(translation => translation.ChapterTitle).HasMaxLength(500);
        builder.Property(translation => translation.PageNumber);
        builder.Property(translation => translation.SavedAt).IsRequired();

        builder.HasIndex(translation => new { translation.UserId, translation.SavedAt });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(translation => translation.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}