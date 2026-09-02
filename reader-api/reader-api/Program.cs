using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Reader.Api.Application.Ports;
using Reader.Api.Application.UseCases;
using reader_api.Authentication;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt must be configured.");
jwtOptions.Validate();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuthenticateUserHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<LocalCredentialStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<JwtUserSynchronizer>();
builder.Services.AddScoped<GetUserProfileHandler>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
