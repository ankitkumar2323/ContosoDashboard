using Microsoft.AspNetCore.Http;

namespace ContosoDashboard.Pages;

internal sealed class BrowserFileFormFile : IFormFile, IDisposable
{
    private readonly Stream _stream;

    public BrowserFileFormFile(Stream stream, string name, string contentType, long length)
    {
        _stream = stream;
        FileName = name;
        ContentType = contentType;
        Length = length;
    }

    public string ContentDisposition => string.Empty;
    public string ContentType { get; }
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length { get; }
    public string Name => "file";
    public string FileName { get; }
    public Stream OpenReadStream() => _stream;
    public void CopyTo(Stream target) => _stream.CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
    public void Dispose() => _stream.Dispose();
}