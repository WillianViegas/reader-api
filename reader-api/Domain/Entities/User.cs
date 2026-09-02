namespace Reader.Api.Domain.Entities;

public sealed class User
{
    public User(Guid id, string externalSubject, string displayName, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new ArgumentException("An external subject is required.", nameof(externalSubject));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        Id = id;
        ExternalSubject = externalSubject.Trim();
        DisplayName = displayName.Trim();
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; }

    public string ExternalSubject { get; }

    public string DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateDisplayName(string displayName, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
        UpdatedAt = updatedAt;
    }
}