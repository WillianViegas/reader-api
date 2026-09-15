namespace Reader.Api.Domain.Entities;

public sealed class User
{
    public User(Guid id, string email, string displayName, string passwordHash, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("An email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        Id = id;
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public User(Guid id, string externalSubject, string displayName, DateTimeOffset createdAt)
        : this(id, externalSubject, displayName, "legacy-external-identity", createdAt)
    {
    }

    public Guid Id { get; }

    public string Email { get; private set; }

    public string ExternalSubject => Email;

    public string PasswordHash { get; private set; }

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

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }
}