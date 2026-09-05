using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Reader.Api.Infrastructure.MangaDex;

// Typed HTTP client for api.mangadex.org. Applies a retry policy for transient
// failures only (timeouts, 5xx, 429 with backoff) and converts exhaustion into
// ExternalCatalogUnavailableException. 404 responses surface as null so callers
// can represent "external resource not found" without exposing the raw body.
public sealed class MangaDexApiClient(HttpClient httpClient, ILogger<MangaDexApiClient> logger)
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    public async Task<T?> GetAsync<T>(string relativeUri, int maxRetryAttempts, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.GetAsync(relativeUri, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                response?.Dispose();

                if (attempt < maxRetryAttempts - 1 && cancellationToken.IsCancellationRequested is false)
                {
                    logger.LogWarning("Transient failure calling MangaDex (attempt {Attempt}); retrying.", attempt + 1);
                    await DelayAsync(attempt, cancellationToken);
                    continue;
                }

                throw new ExternalCatalogUnavailableException("The external catalog is unavailable.", exception);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }

                if (IsTransient(response.StatusCode))
                {
                    if (attempt < maxRetryAttempts - 1)
                    {
                        logger.LogWarning(
                            "MangaDex responded {StatusCode} (attempt {Attempt}); retrying.",
                            (int)response.StatusCode,
                            attempt + 1);
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw new ExternalCatalogUnavailableException($"The external catalog responded with a transient failure ({(int)response.StatusCode}).");
                }

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException exception)
                {
                    throw new ExternalCatalogUnavailableException($"The external catalog responded with an error ({(int)response.StatusCode}).", exception);
                }

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            }
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.GatewayTimeout ||
        statusCode == HttpStatusCode.ServiceUnavailable ||
        statusCode == HttpStatusCode.BadGateway;

    private static Task DelayAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)], cancellationToken);
}
