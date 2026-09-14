# Research: Document Upload and Management

## Decision: Extend the existing single Blazor Server application

**Rationale**: The repository already separates models, EF Core data access, services, and Razor components. Keeping the feature in those boundaries avoids a major rewrite and preserves the current mock-authenticated training workflow.

**Alternatives considered**: A separate frontend/API pair was rejected because it adds deployment and authorization boundaries without being required by the feature.

## Decision: Local filesystem storage behind `IFileStorageService`

**Rationale**: The constitution requires offline-first operation, while the stakeholder requirements require a future Azure Blob migration path. A storage interface lets the training implementation use `AppData/uploads` and keeps business logic independent of the storage provider.

**Alternatives considered**: Direct file I/O in pages or services was rejected because it would make authorization, cleanup, and Azure migration harder to test and replace.

## Decision: Quarantine-first malware scanning

**Rationale**: Uploads must never become visible before a successful scan. The upload workflow writes a generated path to quarantine, scans through a replaceable service, promotes only clean files, and leaves pending or scanner-unavailable files inaccessible.

**Alternatives considered**: Immediate visibility with asynchronous scanning was rejected because it violates the clarified security decision. Rejecting all uploads offline was rejected because the training feature must remain usable without cloud services.

## Decision: Azure Queue Storage and Azure Functions for cloud scan processing

**Rationale**: The upload request should finish after durable quarantine storage and queue publication rather than waiting for malware scanning. An Azure Queue Storage message identifies the quarantined document, and an Azure Function with a Queue Storage trigger performs the scan asynchronously, retries transient failures, and promotes only clean files. The function is idempotent and records rejected or unavailable outcomes without exposing content.

**Alternatives considered**: Scanning inside the Blazor request was rejected because it increases request latency and couples user uploads to scanner availability. A continuously running worker service was rejected for the cloud path because Queue-triggered Functions provide managed activation and retry behavior. Azure services are optional for offline training, where local adapters implement the same interfaces.

## Decision: Service-layer authorization with protected file endpoints

**Rationale**: Existing services already enforce project membership and user isolation. Document operations will follow that pattern, while a narrow controller will return file bytes only after the same authorization check. User-supplied filenames will never become storage paths.

**Alternatives considered**: Static file hosting was rejected because it bypasses per-document authorization and creates an IDOR risk.

## Decision: Metadata search with targeted indexes

**Rationale**: The initial scope explicitly requires search over title, description, tags, uploader, and project, not file content. SQLite indexes and bounded queries support the 2-second target without introducing a search platform.

**Alternatives considered**: Full-text content indexing and an external search service were rejected as out of scope and inconsistent with offline-first simplicity.

## Decision: Integer keys and text categories

**Rationale**: The stakeholder requirements require integer document identifiers and text category values to match the existing EF Core model and training-friendly schema.

**Alternatives considered**: GUID primary keys and enum-backed category storage were rejected because they conflict with the stated data constraints.
