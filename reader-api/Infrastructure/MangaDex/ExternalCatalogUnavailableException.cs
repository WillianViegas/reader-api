namespace Reader.Api.Infrastructure.MangaDex;

public sealed class ExternalCatalogUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public const string Code = "ExternalCatalogUnavailable";
}
