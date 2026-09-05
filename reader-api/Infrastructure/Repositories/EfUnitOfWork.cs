using Reader.Api.Application.Ports;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace Reader.Api.Infrastructure.Repositories;

public sealed class EfUnitOfWork(ReaderDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
