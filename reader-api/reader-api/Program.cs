using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using Reader.Api.Domain.Exceptions;
using Reader.Api.Infrastructure.DatabaseConfig;
using Reader.Api.Infrastructure.MangaDex;
using Reader.Api.Infrastructure.Repositories;
using reader_api.Authentication;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt must be configured.");
jwtOptions.Validate();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    // Registra o esquema Bearer JWT para que o Swagger UI exiba o botão Authorize.
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Cole o token JWT (somente o token, sem o prefixo 'Bearer ')."
        };
        return Task.CompletedTask;
    });
    // Aplica o requisito Bearer às operações, exibindo o cadeado em cada rota.
    options.AddOperationTransformer((operation, _, _) =>
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer")] = []
        });
        return Task.CompletedTask;
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ReaderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReaderDb")
        ?? "Host=localhost;Port=5432;Database=reader_api;Username=postgres;Password=postgres"));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ILibraryRepository, EfLibraryRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton<IClock, SystemClock>();

var mangaDexOptions = builder.Configuration.GetSection(MangaDexOptions.SectionName).Get<MangaDexOptions>() ?? new MangaDexOptions();
builder.Services.AddSingleton(mangaDexOptions);
builder.Services.AddHttpClient<MangaDexApiClient>(client =>
{
    client.BaseAddress = new Uri(mangaDexOptions.BaseAddress, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(mangaDexOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(mangaDexOptions.UserAgent);
});
builder.Services.AddHttpClient("MangaDexCovers", client =>
{
    client.BaseAddress = new Uri(mangaDexOptions.CoversBaseAddress, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(mangaDexOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(mangaDexOptions.UserAgent);
});
builder.Services.AddTransient<ICatalogProvider, MangaDexCatalogProvider>();
builder.Services.AddTransient<IChapterProvider, MangaDexChapterProvider>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuthenticateUserHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<LocalCredentialStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<JwtUserSynchronizer>();
builder.Services.AddScoped<GetUserProfileHandler>();
builder.Services.AddScoped<AddMangaToLibraryHandler>();
builder.Services.AddScoped<RemoveMangaFromLibraryHandler>();
builder.Services.AddScoped<SetMangaFavoriteHandler>();
builder.Services.AddScoped<RegisterReadingProgressHandler>();
builder.Services.AddScoped<CompleteChapterHandler>();
builder.Services.AddScoped<GetLibraryHandler>();
builder.Services.AddScoped<GetContinueReadingHandler>();
builder.Services.AddScoped<GetReadingProgressHandler>();
builder.Services.AddScoped<SearchCatalogHandler>();
builder.Services.AddScoped<GetMangaDetailsHandler>();
builder.Services.AddScoped<GetMangaChaptersHandler>();
builder.Services.AddScoped<GetChapterPagesHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            NameClaimType = ClaimTypes.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var synchronizer = context.HttpContext.RequestServices.GetRequiredService<JwtUserSynchronizer>();
                try
                {
                    var userId = await synchronizer.SynchronizeAsync(context.Principal!, context.HttpContext.RequestAborted);
                    ((ClaimsIdentity)context.Principal!.Identity!).AddClaim(new Claim(ReaderClaimTypes.UserId, userId.ToString("D")));
                }
                catch (SecurityTokenException exception)
                {
                    context.Fail(exception.Message);
                }
            }
        };
    });
builder.Services.AddAuthorization();

const string CorsPolicy = "WebClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "API v1");
});

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ReaderDbContext>();
    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.MigrateAsync();
    }
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

// Serve the static web client (web/) at the site root when the folder is present.
// In development it lives at <contentRoot>/../web; in the container it is copied next to the app.
var webRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "web"));
if (!Directory.Exists(webRoot))
{
    webRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "web"));
}
if (Directory.Exists(webRoot))
{
    var webProvider = new PhysicalFileProvider(webRoot);
    var staticFiles = new StaticFileOptions { FileProvider = webProvider, RequestPath = "" };
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = webProvider, RequestPath = "" });
    app.UseStaticFiles(staticFiles);
}

// Translates domain/application failures into Problem Details (500 fallback).
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var exception = feature?.Error;

    var (status, title) = exception switch
    {
        LibraryItemAlreadyExistsException => (StatusCodes.Status409Conflict, "Library item already exists"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        DomainException => (StatusCodes.Status400BadRequest, "Domain rule violation"),
        ExternalCatalogUnavailableException => (StatusCodes.Status503ServiceUnavailable, "External catalog unavailable"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };

    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = status == StatusCodes.Status500InternalServerError ? null : exception?.Message,
        Instance = context.Request.Path
    };

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    var json = System.Text.Json.JsonSerializer.Serialize(problem);
    await context.Response.WriteAsync(json);
}));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

public partial class Program;
