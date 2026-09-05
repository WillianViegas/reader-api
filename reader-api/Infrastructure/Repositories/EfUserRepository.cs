using Microsoft.EntityFrameworkCore;
using Reader.Api.Application.Ports;
using Reader.Api.Domain.Entities;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace Reader.Api.Infrastructure.Repositories;

public sealed class EfUserRepository(ReaderDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByExternalSubjectAsync(string externalSubject, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.ExternalSubject == externalSubject, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }
}
