using Microsoft.AspNetCore.Http;

namespace ContosoDashboard.Services;

public sealed record DocumentSearchFilters(
    string? Query = null,
    string? Category = null,
    int? ProjectId = null,
    DateTime? From = null,
    DateTime? To = null,
    string SortBy = "UploadedDate");

public sealed record DocumentUploadResult(
    bool Succeeded,
    string FileName,
    int? DocumentId,
    string Message);

public sealed record ScanQueueMessage(int DocumentId, string StoragePath, string AttemptId, string? PreviousStoragePath = null);

public sealed record DocumentShareResult(bool Succeeded, string Message);

public interface IScanQueue
{
    Task EnqueueAsync(ScanQueueMessage message, CancellationToken cancellationToken = default);
}

public interface IMalwareScanningService
{
    Task<ScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}

public sealed record ScanResult(bool IsClean, bool IsUnavailable, string? Message = null);

public interface IFileStorageService
{
    Task<string> SaveQuarantineAsync(IFormFile file, string relativePath, CancellationToken cancellationToken = default);
    Task PromoteAsync(string quarantinePath, string availablePath, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
