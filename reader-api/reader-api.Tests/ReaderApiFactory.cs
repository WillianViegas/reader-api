using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reader.Api.Infrastructure.DatabaseConfig;

namespace reader_api.Tests;

public class ReaderApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"reader-api-tests-{Guid.NewGuid():N}.db");

    public ReaderDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<ReaderDbContext>();

    public void ReplaceService<TService>(TService instance) where TService : class => overrides[typeof(TService)] = instance;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            var descriptorsToRemove = services
                .Where(descriptor => IsEntityFrameworkRegistration(descriptor) || descriptor.ServiceType == typeof(ReaderDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ReaderDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath};Pooling=false"));

            foreach (var (serviceType, instance) in overrides)
            {
                var descriptor = services.SingleOrDefault(item => item.ServiceType == serviceType);
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(serviceType, instance);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of the temporary test database.
        }
    }

    private readonly Dictionary<Type, object> overrides = [];

    private static bool IsEntityFrameworkRegistration(ServiceDescriptor descriptor)
    {
        static bool IsEntityFrameworkType(Type? type) =>
            type?.FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true ||
            type?.FullName?.StartsWith("Npgsql", StringComparison.Ordinal) == true;

        return descriptor.ServiceType == typeof(DbContextOptions)
            || descriptor.ServiceType == typeof(DbContextOptions<ReaderDbContext>)
            || IsEntityFrameworkType(descriptor.ServiceType)
            || IsEntityFrameworkType(descriptor.ImplementationType);
    }
}
