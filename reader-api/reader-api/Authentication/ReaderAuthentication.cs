using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Entities;

namespace reader_api.Authentication;

public static class ReaderClaimTypes
{
    public const string UserId = "reader_user_id";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public required string Email { get; init; }

    public required string Password { get; init; }

    public int LifetimeMinutes { get; init; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience) || SigningKey.Length < 32 || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || LifetimeMinutes < 1)
        {
            throw new InvalidOperationException("Jwt configuration is invalid.");
        }
    }
}

public sealed class LocalCredentialStore
{
    private readonly ConcurrentDictionary<string, string> passwordHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly PasswordHasher<string> passwordHasher = new();

    public LocalCredentialStore(JwtOptions options)
    {
        var email = NormalizeEmail(options.Email);
        passwordHashes[email] = passwordHasher.HashPassword(email, options.Password);
    }

    public bool Register(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        return passwordHashes.TryAdd(normalizedEmail, passwordHasher.HashPassword(normalizedEmail, password));
    }

    public bool IsValid(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        return passwordHashes.TryGetValue(normalizedEmail, out var passwordHash) &&
            passwordHasher.VerifyHashedPassword(normalizedEmail, passwordHash, password) != PasswordVerificationResult.Failed;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        return address.Address.ToLowerInvariant();
    }
}

public sealed class JwtTokenService(JwtOptions options, IClock clock)
{
    public string Create(string email)
    {
        var now = clock.UtcNow;
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            [new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Name, email)],
            now.UtcDateTime,
            now.AddMinutes(options.LifetimeMinutes).UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class JwtUserSynchronizer(AuthenticateUserHandler authenticateUser)
{
    public async Task<Guid> SynchronizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new SecurityTokenException("The JWT does not contain an email claim.");
        }

        var displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var user = await authenticateUser.HandleAsync(new AuthenticateUserCommand(email, displayName), cancellationToken);
        return user.Id;
    }
}

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ReaderClaimTypes.UserId);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("The authenticated request has no local user.");
        }
    }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> usersById = new();
    private readonly ConcurrentDictionary<string, Guid> userIdsBySubject = new(StringComparer.Ordinal);

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!userIdsBySubject.TryAdd(user.ExternalSubject, user.Id))
        {
            throw new InvalidOperationException("A user with this external subject already exists.");
        }

        usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<User?> GetByExternalSubjectAsync(string externalSubject, CancellationToken cancellationToken = default)
    {
        var user = userIdsBySubject.TryGetValue(externalSubject, out var userId) && usersById.TryGetValue(userId, out var found)
            ? found
            : null;
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        usersById.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }
}