using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using reader_api.Controllers;

namespace reader_api.Tests;

public class UsersEndpointTests
{
    [Fact]
    public async Task GetMe_WithoutAToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();

        var response = await factory.CreateClient().GetAsync("/api/users/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAnInvalidToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAValidIdentity_CreatesAndReturnsTheLocalUser()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users/me");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("reader@example.test", body);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("reader@example.test", "wrong-password"));

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesAnAccountAndReturnsATokenForTheNewProfile()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var registration = new RegisterRequest("new-reader@example.test", "secure-password", "New Reader");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registration);
        var login = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(System.Net.HttpStatusCode.Created, registerResponse.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Assert.IsType<LoginResponse>(login).AccessToken);
        var profileResponse = await client.GetAsync("/api/users/me");
        var profile = await profileResponse.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Contains("new-reader@example.test", profile);
        Assert.Contains("New Reader", profile);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_ReturnsConflict()
    {
        using var factory = CreateFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("reader@example.test", "secure-password", "Duplicate Reader"));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAShortPassword_ReturnsBadRequest()
    {
        using var factory = CreateFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("new-reader@example.test", "short", "New Reader"));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("reader@example.test", "reader-development-password"));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        return Assert.IsType<LoginResponse>(login).AccessToken;
    }
}