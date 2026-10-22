namespace Reader.Api.Infrastructure.S3;

public sealed class PdfStorageOptions
{
    public const string SectionName = "PdfStorage";

    public string? BucketName { get; init; }

    public string Region { get; init; } = "us-east-1";

    public string? ServiceUrl { get; init; }
}
