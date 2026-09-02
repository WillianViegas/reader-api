namespace Reader.Api.Domain.Exceptions;

public sealed class LibraryItemAlreadyExistsException(string message) : DomainException(message);