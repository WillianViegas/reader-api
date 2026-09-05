using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reader.Api.Infrastructure.DatabaseConfig;

public sealed class ReaderDbContextFactory : IDesignTimeDbContextFactory<ReaderDbContext>
{
    public ReaderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReaderDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=reader_api;Username=postgres;Password=postgres")
            .Options;

        return new ReaderDbContext(options);
    }
}
