using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class LocalDocumentScanWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LocalScanQueue _queue;

    public LocalDocumentScanWorker(IServiceScopeFactory scopeFactory, IScanQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue as LocalScanQueue ?? throw new InvalidOperationException("Local scan queue is required for the training worker.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var message) && message is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                var scanner = scope.ServiceProvider.GetRequiredService<IMalwareScanningService>();
                var audit = scope.ServiceProvider.GetRequiredService<DocumentAuditService>();
                var document = await context.Documents.FindAsync([message.DocumentId], stoppingToken);
                if (document is null || document.Status != DocumentStatuses.PendingScan) continue;
                ScanResult result;
                await using (var content = await storage.OpenReadAsync(message.StoragePath, stoppingToken))
                {
                    if (content is null) continue;
                    result = await scanner.ScanAsync(content, document.FileName, stoppingToken);
                }
                if (result.IsUnavailable) continue;
                document.ScannedDate = DateTime.UtcNow;
                if (!result.IsClean)
                {
                    document.Status = DocumentStatuses.Rejected;
                    await context.SaveChangesAsync(stoppingToken);
                    await audit.RecordAsync(document.UploaderId, document.DocumentId, "Scan", "Rejected", result.Message);
                    continue;
                }
                var availablePath = message.StoragePath.Replace(Path.Combine("quarantine", ""), "available" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                await storage.PromoteAsync(message.StoragePath, availablePath, stoppingToken);
                document.FilePath = availablePath;
                document.Status = DocumentStatuses.Available;
                await context.SaveChangesAsync(stoppingToken);
                if (!string.IsNullOrWhiteSpace(message.PreviousStoragePath) && !string.Equals(message.PreviousStoragePath, availablePath, StringComparison.OrdinalIgnoreCase))
                    await storage.DeleteAsync(message.PreviousStoragePath, stoppingToken);
                await audit.RecordAsync(document.UploaderId, document.DocumentId, "Scan", "Available");
            }
            else
            {
                await Task.Delay(250, stoppingToken);
            }
        }
    }
}
