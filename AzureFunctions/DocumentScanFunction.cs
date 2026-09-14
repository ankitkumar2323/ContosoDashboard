using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions;

public class DocumentScanFunction
{
    private readonly ILogger<DocumentScanFunction> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public DocumentScanFunction(BlobServiceClient blobServiceClient, ILogger<DocumentScanFunction> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    [Function("DocumentScanFunction")]
    public async Task Run([QueueTrigger("document-scan-queue", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Received scan message: {Message}", message);

        var payload = JsonSerializer.Deserialize<DocumentScanMessage>(message);
        if (payload is null)
        {
            _logger.LogWarning("Queue message did not deserialize to a valid document payload.");
            return;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("DocumentBlobContainer") ?? "document-files");
        var blobClient = containerClient.GetBlobClient(payload.StoragePath);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Expected quarantined blob {StoragePath} was not found.", payload.StoragePath);
            return;
        }

        // This training implementation intentionally keeps the Azure function implementation bounded and safe.
        // A production scanner would read the quarantined blob, invoke the malware service, and update the document state.
        _logger.LogInformation("Document {DocumentId} is queued for scan processing in the Azure path.", payload.DocumentId);
    }
}

public sealed record DocumentScanMessage(int DocumentId, string StoragePath, string AttemptId, string? PreviousStoragePath = null);
