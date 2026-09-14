# Quickstart: Document Upload and Management

## Prerequisites

- .NET 10 SDK
- Windows or Linux local filesystem access
- Existing ContosoDashboard SQLite configuration
- No cloud service is required for the training implementation

## Planned configuration

1. Create an application data directory outside `wwwroot`, for example `AppData/uploads`.
2. Configure the local storage root and maximum file size through application configuration.
3. Register the local `IFileStorageService` and offline malware scanner implementation in dependency injection.
4. Apply the EF Core schema changes for documents, shares, task links, and audit events.

## Azure scan-processing deployment

For the cloud deployment path:

1. Configure Azure Blob Storage for quarantine and available file containers with encryption at rest.
2. Configure an Azure Queue Storage queue for document scan messages and a retry/dead-letter policy.
3. Deploy `DocumentScanFunction` with a Queue Storage trigger and permissions to read quarantine content, invoke the scanner, update document status, and write audit events.
4. Configure the application to publish `{documentId, storagePath, attemptId}` messages after quarantine upload; never put file bytes or secrets in the message.
5. Verify that clean files become available, malware remains rejected, scanner outages keep documents inaccessible, and duplicate queue delivery is idempotent.

The offline training path uses local storage and local queue/scanner adapters and does not require Azure resources.

## Verification workflow

1. Start the application with `dotnet run` from `ContosoDashboard`.
2. Sign in with one of the existing mock users.
3. Upload a supported file smaller than 25 MB and confirm it is initially unavailable while scanning is pending.
4. Confirm a clean file appears in My Documents only after the scan succeeds.
5. Try an unsupported, oversized, or scanner-unavailable upload and confirm it remains inaccessible.
6. Search by title, category, tag, uploader, and project; confirm results exclude unauthorized documents.
7. Download and preview an authorized document, then confirm the audit entries.
8. Share with an already-authorized project member and confirm the notification.
9. Attach a document to a task and confirm the project context is preserved.
10. Confirm the dashboard Recent Documents widget and document count update.
11. As an administrator, confirm activity reports include uploads, downloads, replacements, deletions, and shares.

## Performance checks

- Upload a 25 MB supported file under typical network conditions and record completion time.
- Load a list containing up to 500 documents and record page response time.
- Run representative metadata searches and record response time.
- Preview a representative PDF and image and record time to display.

## Security checks

- Attempt to access another user's document by changing the document identifier in the URL.
- Attempt to access a project document as a non-member.
- Attempt to share with a user outside the authorized project or role boundary.
- Confirm original filenames are not used as storage paths and files are not served from `wwwroot`.
