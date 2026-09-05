using System.Net.Http.Headers;
using System.Net.Http.Json;
using reader_api.Controllers;

namespace reader_api.Tests;

public static class ReaderApiClientExtensions
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this ReaderApiFactory factory, string email = "reader@example.test")
    {
        var client = factory.CreateClient();

        if (email == "reader@example.test")
        {
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "reader-development-password"));
            login.EnsureSuccessStatusCode();
            var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Assert.IsType<LoginResponse>(loginBody).AccessToken);
            return client;
        }

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "secure-password", "Other Reader"));
        register.EnsureSuccessStatusCode();
        var registerBody = await register.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Assert.IsType<LoginResponse>(registerBody).AccessToken);
        return client;
    }
}
