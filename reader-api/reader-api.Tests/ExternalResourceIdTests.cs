using Reader.Api.Domain.Enums;
using Reader.Api.Domain.ValueObjects;

namespace reader_api.Tests;

public class ExternalResourceIdTests
{
    [Fact]
    public void Create_WithSameProviderAndValue_ProducesEqualValueObjects()
    {
        var first = new ExternalResourceId(ExternalCatalogProvider.MangaDex, "  manga-123  ");
        var second = new ExternalResourceId(ExternalCatalogProvider.MangaDex, "manga-123");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new ExternalResourceId(ExternalCatalogProvider.MangaDex, value));
    }
}