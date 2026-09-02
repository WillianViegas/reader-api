using Reader.Api.Domain.Enums;

namespace Reader.Api.Domain.ValueObjects;

public sealed record ExternalResourceId
{
    public ExternalResourceId(ExternalCatalogProvider provider, string value)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new ArgumentOutOfRangeException(nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An external resource identifier is required.", nameof(value));
        }

        Provider = provider;
        Value = value.Trim();
    }

    public ExternalCatalogProvider Provider { get; }

    public string Value { get; }
}