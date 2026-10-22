using Amazon.S3;
using Amazon.S3.Model;
using Reader.Api.Application.Ports;

namespace Reader.Api.Infrastructure.S3;

public sealed class S3PdfStorageProvider(IAmazonS3 s3, PdfStorageOptions options) : IPdfStorageProvider
{
    public async Task StoreAsync(Guid userId, Guid pdfId, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = GetBucketName(),
            Key = GetObjectKey(userId, pdfId),
            InputStream = content,
            ContentType = "application/pdf",
            AutoCloseStream = false
        }, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default) =>
        OpenObjectAsync(GetObjectKey(userId, pdfId), cancellationToken);

    public async Task StoreCoverAsync(Guid userId, Guid pdfId, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = GetBucketName(),
            Key = GetCoverKey(userId, pdfId),
            InputStream = content,
            ContentType = "image/jpeg",
            AutoCloseStream = false
        }, cancellationToken);
    }

    public Task<Stream?> OpenCoverAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default) =>
        OpenObjectAsync(GetCoverKey(userId, pdfId), cancellationToken);

    public async Task DeleteAsync(Guid userId, Guid pdfId, CancellationToken cancellationToken = default)
    {
        await s3.DeleteObjectAsync(GetBucketName(), GetObjectKey(userId, pdfId), cancellationToken);
        await s3.DeleteObjectAsync(GetBucketName(), GetCoverKey(userId, pdfId), cancellationToken);
    }

    private async Task<Stream?> OpenObjectAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3.GetObjectAsync(GetBucketName(), key, cancellationToken);
            return new OwnedS3ResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    private static string GetCoverKey(Guid userId, Guid pdfId) =>
        $"users/{userId:D}/pdfs/{pdfId:D}.jpg";

    private string GetBucketName() =>
        !string.IsNullOrWhiteSpace(options.BucketName)
            ? options.BucketName
            : throw new InvalidOperationException("PdfStorage:BucketName must be configured.");

    private static string GetObjectKey(Guid userId, Guid pdfId) =>
        $"users/{userId:D}/pdfs/{pdfId:D}.pdf";

    private sealed class OwnedS3ResponseStream(GetObjectResponse response) : Stream
    {
        private Stream Content => response.ResponseStream;

        public override bool CanRead => Content.CanRead;
        public override bool CanSeek => Content.CanSeek;
        public override bool CanWrite => Content.CanWrite;
        public override long Length => Content.Length;

        public override long Position
        {
            get => Content.Position;
            set => Content.Position = value;
        }

        public override void Flush() => Content.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            Content.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => Content.Read(buffer);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            Content.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            Content.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => Content.Seek(offset, origin);

        public override void SetLength(long value) => Content.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            Content.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            response.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
