using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Reader.Api.Application.Dtos;

namespace Reader.Api.Infrastructure.MangaDex;

// Typed HTTP client for api.mangadex.org. Retries only bounded transient failures
// and converts provider failures into ExternalCatalogUnavailableException.
public sealed class MangaDexApiClient(HttpClient httpClient, ILogger<MangaDexApiClient> logger)
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];
    private const int MaximumImageSizeBytes = 25 * 1024 * 1024;
    private const int MaximumRetryAttempts = 4;

    public Task<ChapterPageImageDto> GetImageAsync(Uri imageUri, int maxRetryAttempts, CancellationToken cancellationToken) =>
        GetWithRetryAsync(
            imageUri,
            "The manga page image",
            maxRetryAttempts,
            HttpCompletionOption.ResponseHeadersRead,
            async (response, token) =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ExternalCatalogUnavailableException("The external chapter page was not found (HTTP 404).");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new ExternalCatalogUnavailableException(
                        $"The external chapter page host returned an error (HTTP {(int)response.StatusCode}).");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) is not true)
                {
                    throw new ExternalCatalogUnavailableException("The external chapter resource was not an image.");
                }

                if (response.Content.Headers.ContentLength is > MaximumImageSizeBytes)
                {
                    throw new ExternalCatalogUnavailableException("The manga page image is too large to proxy.");
                }

                var content = await response.Content.ReadAsByteArrayAsync(token);
                if (content.Length > MaximumImageSizeBytes)
                {
                    throw new ExternalCatalogUnavailableException("The manga page image is too large to proxy.");
                }

                return new ChapterPageImageDto(content, contentType);
            },
            cancellationToken);

    public Task<T?> GetAsync<T>(string relativeUri, int maxRetryAttempts, CancellationToken cancellationToken) =>
        GetWithRetryAsync<T?>(
            new Uri(httpClient.BaseAddress ?? throw new InvalidOperationException("MangaDex client base address is not configured."), relativeUri),
            "The external catalog",
            maxRetryAttempts,
            HttpCompletionOption.ResponseContentRead,
            async (response, token) =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new ExternalCatalogUnavailableException(
                        $"The external catalog responded with an error (HTTP {(int)response.StatusCode}).");
                }

                try
                {
                    return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token);
                }
                catch (System.Text.Json.JsonException exception)
                {
                    throw new ExternalCatalogUnavailableException("The external catalog returned an invalid response.", exception);
                }
            },
            cancellationToken);

    private async Task<TResponse> GetWithRetryAsync<TResponse>(
        Uri requestUri,
        string resourceName,
        int maxRetryAttempts,
        HttpCompletionOption completionOption,
        Func<HttpResponseMessage, CancellationToken, Task<TResponse>> handleResponse,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Clamp(maxRetryAttempts, 1, MaximumRetryAttempts);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(requestUri, completionOption, cancellationToken);
            }
            catch (Exception exception) when (IsTransientTransportFailure(exception))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                if (attempt + 1 == attempts)
                {
                    throw new ExternalCatalogUnavailableException(
                        $"{resourceName} is unavailable after {attempts} attempts ({exception.GetType().Name}).",
                        exception);
                }

                LogRetry(resourceName, attempt, attempts, exception.GetType().Name, null);
                await DelayAsync(attempt, null, cancellationToken);
                continue;
            }

            using (response)
            {
                if (IsTransient(response.StatusCode))
                {
                    var statusCode = response.StatusCode;
                    var retryAfter = response.Headers.RetryAfter;
                    var retryDelay = GetRetryDelay(attempt, retryAfter);

                    if (attempt + 1 == attempts)
                    {
                        throw new ExternalCatalogUnavailableException(
                            $"{resourceName} returned a transient failure (HTTP {(int)statusCode}) after {attempts} attempts.");
                    }

                    LogRetry(resourceName, attempt, attempts, null, (int)statusCode, retryDelay);
                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                try
                {
                    return await handleResponse(response, cancellationToken);
                }
                catch (Exception exception) when (IsTransientTransportFailure(exception))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    if (attempt + 1 == attempts)
                    {
                        throw new ExternalCatalogUnavailableException(
                            $"{resourceName} could not be downloaded completely after {attempts} attempts ({exception.GetType().Name}).",
                            exception);
                    }

                    LogRetry(resourceName, attempt, attempts, exception.GetType().Name, null);
                    await DelayAsync(attempt, null, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException("The external request retry loop ended unexpectedly.");
    }

    private void LogRetry(
        string resourceName,
        int attempt,
        int maxAttempts,
        string? errorType,
        int? statusCode,
        TimeSpan? delay = null)
    {
        logger.LogWarning(
            "Transient failure for {Resource} (attempt {Attempt}/{MaxAttempts}, status {StatusCode}, error {ErrorType}); retrying after {Delay}.",
            resourceName,
            attempt + 1,
            maxAttempts,
            statusCode,
            errorType,
            delay ?? RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)]);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.InternalServerError ||
        statusCode == HttpStatusCode.GatewayTimeout ||
        statusCode == HttpStatusCode.ServiceUnavailable ||
        statusCode == HttpStatusCode.BadGateway;

    private static bool IsTransientTransportFailure(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or IOException;

    private static TimeSpan GetRetryDelay(int attempt, System.Net.Http.Headers.RetryConditionHeaderValue? retryAfter)
    {
        var delay = RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)];
        var providerDelay = retryAfter?.Delta
            ?? (retryAfter?.Date is { } retryAt ? retryAt - DateTimeOffset.UtcNow : null);
        return providerDelay is { } value && value > delay ? value : delay;
    }

    private static Task DelayAsync(
        int attempt,
        System.Net.Http.Headers.RetryConditionHeaderValue? retryAfter,
        CancellationToken cancellationToken) =>
        Task.Delay(GetRetryDelay(attempt, retryAfter), cancellationToken);
}
