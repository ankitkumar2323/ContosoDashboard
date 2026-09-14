# Implementation Plan: Document Upload and Management

**Branch**: `006-document-management` | **Date**: 2026-09-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-document-management/spec.md`

## Summary

Add secure document upload, metadata management, search, sharing, task/dashboard integration, and audit reporting to the existing ContosoDashboard Blazor Server application. The implementation extends the current EF Core SQLite model and service layer, stores files outside `wwwroot` behind `IFileStorageService`, and keeps uploads quarantined until a replaceable malware-scanning service succeeds. In the Azure deployment path, the upload service publishes a scan message to Azure Queue Storage and an Azure Function with a Queue Storage trigger performs asynchronous scanning and promotes only clean files.

## Technical Context

**Language/Version**: C# / .NET 10.0, ASP.NET Core Blazor Server  
**Primary Dependencies**: Entity Framework Core SQLite 10.0; existing authentication, authorization, notification, project, task, and dashboard services; Azure Queue Storage and Azure Functions for the cloud scan worker  
**Storage**: SQLite metadata plus local filesystem under application data outside `wwwroot`; `IFileStorageService` isolates future Azure Blob Storage; Azure Blob Storage and Queue Storage are used by the cloud deployment path  
**Testing**: `dotnet build`; focused service/component verification and manual authenticated workflow checks because no test project is currently present  
**Target Platform**: Offline-capable Windows/Linux ASP.NET Core host; future Azure deployment compatible  
**Project Type**: Single web application  
**Performance Goals**: 25 MB upload within 30 seconds under typical conditions; 500-document lists and metadata searches within 2 seconds; supported previews within 3 seconds  
**Constraints**: Existing mock cookie authentication and role policies; integer document keys; text categories; no files in `wwwroot`; quarantine until malware scan succeeds; no major rewrite; Azure Functions and Queue Storage are optional cloud components and must not be required for offline training  
**Scale/Scope**: Up to 5,000 employees; initial release covers metadata search, project/task/dashboard integration, sharing, notifications, and audit reporting; excluded features remain out of scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

* **Security-First Access Control**: PASS. All document reads and mutations are routed through authorized service operations; project membership, role, and sharing checks are required before access.
* **Quality-Driven Delivery**: PASS. The design uses existing layers, explicit interfaces, focused entities, and verification steps without adding an unnecessary application boundary.
* **Performance & Resilience**: PASS. Query indexes, bounded list results, metadata search, quarantine states, and failure-safe upload ordering address the stated targets.
* **Technical Standards & Compatibility**: PASS. The plan follows the existing .NET 10, EF Core, Blazor, SQLite, and dependency-injection patterns.
* **Offline-First, Replaceable Architecture**: PASS. Local storage and scanner implementations are behind interfaces, with Azure migration documented as a future implementation swap.
* **No constitution violations**: PASS. No complexity exception is required.

### Post-Design Check

* **Security-First Access Control**: PASS. The data model requires authorization-aware service queries, protected content endpoints, quarantine states, and sharing limited to existing access boundaries.
* **Quality-Driven Delivery**: PASS. Research, data model, contract, and quickstart artifacts are explicit and independently reviewable.
* **Performance & Resilience**: PASS. The design includes metadata indexes, bounded document lists, failure-safe upload ordering, and inaccessible pending scan states.
* **Technical Standards & Compatibility**: PASS. The design uses the existing single-project .NET, EF Core, Blazor, and DI structure.
* **Offline-First, Replaceable Architecture**: PASS. Local filesystem and scanner adapters remain replaceable without changing document business rules.
* **No unresolved violations**: PASS.

## Project Structure

### Documentation (this feature)

```text
specs/006-document-management/
├── plan.md
├── research.md
├── data-model.md
├── contracts/document-management.openapi.yaml
└── quickstart.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs             # Document, share, task-link, audit mappings
├── Models/                                   # Document domain entities and value/status types
├── Services/
│   ├── DocumentService.cs                   # Authorized document workflows
│   ├── FileStorageService.cs                # Local storage implementation
│   ├── MalwareScanningService.cs            # Scanner abstraction and offline implementation
│   ├── ScanQueuePublisher.cs                # Publishes cloud scan messages after quarantine upload
│   ├── DocumentAuditService.cs               # Audit event persistence/report queries
│   └── ...existing services                  # Projects, tasks, dashboard, notifications
├── Pages/
│   ├── Documents.razor                      # My Documents, search, filters, management
│   ├── ProjectDetails.razor                 # Project document view and upload entry point
│   ├── Tasks.razor / task detail             # Task attachment integration
│   └── Index.razor                           # Recent Documents and count widget
├── Controllers/DocumentFileController.cs    # Authorized download/preview responses
├── wwwroot/css/site.css                     # Feature styling only where needed
└── Program.cs                                # Dependency injection registrations

AzureFunctions/
└── DocumentScanFunction.cs                  # Queue Storage trigger for asynchronous cloud scanning
```

**Structure Decision**: Extend the existing single `ContosoDashboard` web project for the offline training path. The optional Azure deployment adds a small isolated `AzureFunctions` worker containing the Queue Storage-triggered scan function; it does not move business UI or authorization logic out of the application. Domain entities remain in `Models`, persistence stays in `Data/ApplicationDbContext.cs`, authorization-aware workflows live in `Services`, and a narrow controller serves protected file bytes for download and preview.

## Background Scan Job

1. `DocumentService` validates metadata and file type, writes the file to quarantine storage, creates a `PendingScan` record, and publishes a scan message containing the document identifier and generated storage path.
2. In the Azure deployment, Azure Queue Storage buffers the message and provides retry behavior for transient failures. The message must not contain file bytes or secrets.
3. `DocumentScanFunction` is triggered by the queue, retrieves the quarantined file through the storage abstraction, invokes the malware scanner, and updates the document state to `Available` only for a clean result.
4. Malicious files are marked `Rejected`, remain inaccessible, and generate an audit event. Scanner outages or exhausted retries leave the document non-visible and require an operational retry or dead-letter review.
5. The function must be idempotent: repeated delivery for the same document must not create duplicate records or promote a rejected file. Updates must use a concurrency check on document state.
6. The offline implementation uses the same `IScanQueue` and `IMalwareScanningService` boundaries with an in-process or local queue adapter; it must not require Azure resources.

## Complexity Tracking

> No constitution violations require justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | The feature fits the existing single-project architecture. |
