using Reader.Api.Domain.Entities;

namespace reader_api.Tests;

public class UserTests
{
    [Fact]
    public void UpdateDisplayName_UpdatesNameAndTimestamp()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;
        var user = new User(Guid.NewGuid(), "firebase-subject", "Initial", createdAt);

        user.UpdateDisplayName(" Updated ", updatedAt);

        Assert.Equal("Updated", user.DisplayName);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Create_WithMissingExternalSubject_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(Guid.NewGuid(), " ", "Reader", DateTimeOffset.UtcNow));
    }
}