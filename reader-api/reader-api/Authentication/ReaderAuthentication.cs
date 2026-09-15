using Microsoft.AspNetCore.Identity;
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
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience) || string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32 || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || LifetimeMinutes < 1)
        {
            throw new InvalidOperationException("Jwt configuration is invalid.");
        }
    }
}

public sealed class UserAuthenticationService(IUserRepository users, IUnitOfWork unitOfWork, IClock clock)
{
    private readonly PasswordHasher<string> passwordHasher = new();

    public async Task<User> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        ValidatePassword(password);
        if (await users.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User(Guid.NewGuid(), normalizedEmail, displayName, passwordHasher.HashPassword(normalizedEmail, password), clock.UtcNow);
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(normalizedEmail, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return user;
    }

    public async Task EnsureDevelopmentUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var existing = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is null)
        {
            await RegisterAsync(normalizedEmail, password, normalizedEmail, cancellationToken);
            return;
        }

        if (existing.PasswordHash == "legacy-external-identity" || string.IsNullOrWhiteSpace(existing.PasswordHash))
        {
            existing.SetPasswordHash(passwordHasher.HashPassword(normalizedEmail, password));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        return address.Address.ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("The password must contain at least 8 characters.", nameof(password));
        }
    }
}

public sealed class JwtTokenService(JwtOptions options, IClock clock)
{
    public string Create(User user) => Create(user.Email, user.DisplayName, user.Id);

    public string Create(string email)
        => Create(email, email, null);

    private string Create(string email, string displayName, Guid? userId)
    {
        var now = clock.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName)
        };
        if (userId.HasValue)
        {
            claims.Add(new Claim(ReaderClaimTypes.UserId, userId.Value.ToString("D")));
        }

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
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