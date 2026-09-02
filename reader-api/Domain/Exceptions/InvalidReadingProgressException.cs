namespace Reader.Api.Domain.Exceptions;

public sealed class InvalidReadingProgressException(string message) : DomainException(message);