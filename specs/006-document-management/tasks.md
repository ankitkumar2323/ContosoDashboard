# Tasks: Document Upload and Management

**Input**: Design documents from `specs/006-document-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/document-management.openapi.yaml`
**Tests**: No dedicated test tasks were requested in the feature specification. Verification tasks are included where they validate security, performance, and workflow behavior.
**Organization**: Tasks are grouped by user story and ordered by dependency.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish feature folders, configuration, and dependency registration.

- [X] T001 [P] Add document-management configuration sections for storage root, 25 MB upload limit, quarantine path, and approved categories in `ContosoDashboard/appsettings.json`
- [X] T002 [P] Add development overrides for local document storage and scanner behavior in `ContosoDashboard/appsettings.Development.json`
- [X] T003 [P] Create the Azure Functions worker project structure and Queue Storage trigger configuration in `AzureFunctions/DocumentScanFunction.cs` and `AzureFunctions/host.json`
- [X] T004 Register document, storage, scanner, queue, and audit services in `ContosoDashboard/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build shared persistence, storage, authorization, scanning, and audit infrastructure before user stories.

- [X] T005 [P] Create the `Document` entity with integer key, metadata, generated path, file size, MIME type, project link, uploader, and scan status in `ContosoDashboard/Models/Document.cs`
- [X] T006 [P] Create the `DocumentShare`, `TaskDocument`, and `DocumentAuditEvent` entities in `ContosoDashboard/Models/DocumentShare.cs`, `ContosoDashboard/Models/TaskDocument.cs`, and `ContosoDashboard/Models/DocumentAuditEvent.cs`
- [X] T007 Configure document relationships, text category values, status fields, authorization query indexes, and cascade behavior in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T008 [P] Define `IFileStorageService` and safe relative-path validation in `ContosoDashboard/Services/DocumentContracts.cs`
- [X] T009 Implement local quarantine and available-file storage with GUID-based paths outside `wwwroot` in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T010 [P] Define `IMalwareScanningService` and the offline scanner adapter with pending, clean, rejected, and unavailable outcomes in `ContosoDashboard/Services/DocumentContracts.cs` and `ContosoDashboard/Services/LocalMalwareScanningService.cs`
- [X] T011 [P] Define `IScanQueue` and the local in-process queue adapter for offline training in `ContosoDashboard/Services/DocumentContracts.cs` and `ContosoDashboard/Services/LocalScanQueue.cs`
- [X] T012 Implement authorization helpers for owner, project manager, project member, recipient, and administrator checks in `ContosoDashboard/Services/DocumentAuthorizationService.cs`
- [X] T013 Implement append-only audit event creation and administrator report queries in `ContosoDashboard/Services/DocumentAuditService.cs`
- [X] T014 Create the EF Core schema update for documents, shares, task links, and audit events in `ContosoDashboard/Data/Migrations/DocumentManagementMigration.cs`
- [X] T015 Define the shared document DTOs, upload result, search filters, and scan queue message in `ContosoDashboard/Services/DocumentContracts.cs`

**Checkpoint**: Shared persistence, storage, scanning, queue, authorization, and audit boundaries are ready for story implementation.

---

## Phase 3: User Story 1 - Upload and Organize Documents (Priority: P1) MVP

**Goal**: Allow authenticated users to upload supported files into quarantine, complete scanning, and browse authorized document metadata.

**Independent Test**: Upload supported and invalid files as an authenticated user, confirm per-file progress/results, verify pending files are inaccessible, and confirm clean files appear in the authorized document lists.

### Implementation for User Story 1

- [X] T016 [P] [US1] Implement file type, size, metadata, project-membership, and category validation in `ContosoDashboard/Services/DocumentValidationService.cs`
- [X] T017 [US1] Implement the upload workflow that saves to quarantine before metadata persistence, creates `PendingScan` records, and publishes scan messages in `ContosoDashboard/Services/DocumentService.cs`
	- Depends on: T005-T015 (shared entities, storage, scanning, queue, authorization, audit, and contracts)
	- Note: Return per-file errors for the 25 MB limit, unsupported types, invalid project access, failed storage writes, and unavailable scan queues without exposing quarantined files.
- [X] T018 [US1] Implement offline queue consumption that scans pending documents and promotes clean files without exposing pending or rejected files in `ContosoDashboard/Services/LocalDocumentScanWorker.cs`
	- Depends on: T010-T011 and T017 (scanner, queue, and pending upload workflow)
	- Note: Keep scanner-unavailable and failed-scan documents inaccessible and make repeated queue delivery idempotent.
- [X] T019 [US1] Implement authorized My Documents and Project Documents queries with sorting, filtering, bounded results, and metadata indexes in `ContosoDashboard/Services/DocumentService.cs`
	- Depends on: T005-T007 and T012 (document schema, indexes, and authorization rules)
- [X] T020 [US1] Create the multi-file upload form with metadata fields, per-file progress, validation messages, and scan-pending status in `ContosoDashboard/Pages/Documents.razor`
	- Depends on: T016-T019 (validation, upload, scan, and authorized list workflows)
- [X] T021 [US1] Add Documents navigation and upload entry points to `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T022 [US1] Add the approved category list and upload status styles in `ContosoDashboard/wwwroot/css/site.css`

**Checkpoint**: User Story 1 is independently usable for secure upload, scanning, and authorized browsing.

---

## Phase 4: User Story 2 - Find and Manage Authorized Documents (Priority: P2)

**Goal**: Provide metadata search, preview, download, editing, replacement, and authorized permanent deletion.

**Independent Test**: Search authorized metadata, verify unauthorized results are excluded, preview/download clean files, edit metadata, replace a file, and delete an owned file after confirmation.

### Implementation for User Story 2

- [X] T023 [US2] Add metadata search across title, description, tags, uploader, project, category, and date range in `ContosoDashboard/Services/DocumentService.cs`
	- Depends on: T019 (authorized document query foundation)
- [X] T024 [US2] Implement authorized metadata update, replacement-to-quarantine, and permanent deletion workflows with cleanup handling in `ContosoDashboard/Services/DocumentService.cs`
	- Depends on: T017-T018 and T012 (quarantine workflow, scan processing, and authorization)
	- Note: Replacement files must be scanned before becoming available; cleanup must prevent orphaned files and metadata.
- [X] T025 [US2] Implement protected download and PDF/image preview responses with authorization and unavailable-status checks in `ContosoDashboard/Controllers/DocumentController.cs`
	- Depends on: T012 and T019 (authorization and authorized document retrieval)
	- Note: Never serve pending, rejected, deleted, or scanner-unavailable files, including when a caller changes the document ID.
- [X] T026 [US2] Add search, sort, filter, preview, download, edit, replace, delete-confirmation, and empty/error states to `ContosoDashboard/Pages/Documents.razor`
	- Depends on: T023-T025 (search, management, and protected file operations)
- [X] T027 [US2] Add the protected file route and content-disposition/content-type handling in `ContosoDashboard/Program.cs` and `ContosoDashboard/Controllers/DocumentController.cs`
- [X] T028 [US2] Record preview, download, metadata update, replacement, rejection, and deletion audit events in `ContosoDashboard/Services/DocumentService.cs`
- [X] T029 [US2] Add document list, search controls, preview modal, and management action styling in `ContosoDashboard/wwwroot/css/site.css`

**Checkpoint**: User Stories 1 and 2 are independently usable for secure document discovery and management.

---

## Phase 5: User Story 3 - Share, Integrate, and Audit Document Activity (Priority: P3)

**Goal**: Enable authorized sharing, task attachments, dashboard recent-document visibility, notifications, and administrator reporting.

**Independent Test**: Share with an already-authorized project member, confirm notification and recipient visibility, attach a document to a task, confirm dashboard updates, and generate an administrator audit report.

### Implementation for User Story 3

- [ ] T030 [US3] Implement sharing validation limited to existing project or role authorization and persist `DocumentShare` records in `ContosoDashboard/Services/DocumentService.cs`
	- Depends on: T012, T019, and T024 (authorization, document access, and management workflows)
	- Note: Sharing must not grant access outside the recipient's existing project membership or role permissions.
- [ ] T031 [US3] Create in-app notifications for document shares and newly available project documents using `ContosoDashboard/Services/NotificationService.cs`
	- Depends on: T030 and existing notification persistence in `ContosoDashboard/Services/NotificationService.cs`
- [ ] T032 [US3] Implement task-document attachment and authorized retrieval by task project in `ContosoDashboard/Services/TaskDocumentService.cs`
- [ ] T033 [US3] Add document attachment controls and document list display to task details in `ContosoDashboard/Pages/Tasks.razor`
- [ ] T034 [US3] Add Recent Documents data and document count to `ContosoDashboard/Services/DashboardService.cs`
- [ ] T035 [US3] Add the Recent Documents widget and document count summary card to `ContosoDashboard/Pages/Index.razor`
- [ ] T036 [US3] Add Shared with Me and authorized share controls to `ContosoDashboard/Pages/Documents.razor`
- [ ] T037 [US3] Add administrator document activity report queries and UI in `ContosoDashboard/Services/DocumentAuditService.cs` and `ContosoDashboard/Pages/DocumentAudit.razor`
- [ ] T038 [US3] Implement the Azure Queue Storage publisher and scan message serialization in `ContosoDashboard/Services/AzureScanQueuePublisher.cs`
- [ ] T039 [US3] Implement the Queue Storage-triggered Azure Function with Blob retrieval, malware scan, idempotent state transition, retry-safe handling, and audit updates in `AzureFunctions/DocumentScanFunction.cs`
- [ ] T040 [US3] Add Azure Functions configuration, Queue Storage connection settings, retry/dead-letter settings, and Blob permissions documentation in `AzureFunctions/host.json` and `AzureFunctions/local.settings.json.example`
- [ ] T041 [US3] Add sharing, task attachment, recent-document, notification, and audit report styling in `ContosoDashboard/wwwroot/css/site.css`

**Checkpoint**: All user stories are independently functional and integrated with existing dashboard workflows.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate security, performance, documentation, and deployment readiness across all stories.

- [ ] T042 [P] Document offline and Azure storage/scanning configuration and migration boundaries in `README.md`
- [ ] T043 [P] Add the document management setup, Azure scan worker, security checks, and performance scenarios to `specs/006-document-management/quickstart.md`
- [ ] T044 Review every document query and file endpoint for authentication, project membership, role, share, and IDOR protections in `ContosoDashboard/Services/DocumentAuthorizationService.cs` and `ContosoDashboard/Controllers/DocumentFileController.cs`
- [ ] T045 Validate 25 MB upload, 500-document list, metadata search, and PDF/image preview targets using the scenarios in `specs/006-document-management/quickstart.md`
- [ ] T046 Validate quarantine behavior for malware, scanner outage, queue retry, dead-letter, duplicate delivery, and rejected-file access in `AzureFunctions/DocumentScanFunction.cs` and `ContosoDashboard/Services/LocalDocumentScanWorker.cs`
- [X] T047 Run `dotnet build` from `ContosoDashboard/ContosoDashboard.csproj` and resolve feature-caused compiler errors
- [ ] T048 Review generated database migration, configuration defaults, logging, and secret handling in `ContosoDashboard/Data/Migrations/DocumentManagementMigration.cs`, `ContosoDashboard/appsettings.json`, and `AzureFunctions/local.settings.json.example`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001-T003 can run in parallel, then T004 registers the shared services.
- **Foundational (Phase 2)**: Depends on T004; T005-T006, T008, T010-T011, and T013-T015 can proceed in parallel where files do not overlap. T007 and T014 depend on entity definitions.
- **User Story 1 (Phase 3)**: Depends on all foundational tasks; T016 and T020-T022 can proceed in parallel after shared contracts are available; T017-T019 depend on validation, storage, queue, and persistence foundations.
- **User Story 2 (Phase 4)**: Depends on User Story 1 document entities and service workflow; T023-T025 can proceed in parallel, followed by T026-T029.
- **User Story 3 (Phase 5)**: Depends on User Story 1 and the authorized document service; sharing, task integration, dashboard integration, and Azure worker work can proceed in parallel after their shared contracts are stable.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2 and is the MVP; no dependency on later stories.
- **User Story 2 (P2)**: Depends on US1's document persistence and service workflow, but its search and management behavior is independently testable.
- **User Story 3 (P3)**: Depends on US1's document model and US2's authorized document operations; its sharing, integration, and reporting outcomes are independently testable after those foundations.

### Parallel Opportunities

- T001-T003 can run in parallel during setup.
- T005-T006, T008, T010-T011, and T013-T015 can run in parallel during foundation work.
- T020-T022 can run in parallel with the US1 service implementation once contracts are stable.
- T023-T025 can run in parallel within US2.
- T030-T032, T034, and T038-T040 can run in parallel within US3 when shared service contracts are complete.
- T042-T043 can run in parallel during polish.

---

## Parallel Example: User Story 1

```text
Task: "T016 [US1] Implement file validation in ContosoDashboard/Services/DocumentValidationService.cs"
Task: "T020 [US1] Create the multi-file upload form in ContosoDashboard/Pages/Documents.razor"
Task: "T022 [US1] Add category and upload status styles in ContosoDashboard/wwwroot/css/site.css"
```

## Parallel Example: User Story 2

```text
Task: "T023 [US2] Add metadata search in ContosoDashboard/Services/DocumentService.cs"
Task: "T025 [US2] Implement protected file responses in ContosoDashboard/Controllers/DocumentFileController.cs"
Task: "T029 [US2] Add document management styling in ContosoDashboard/wwwroot/css/site.css"
```

## Parallel Example: User Story 3

```text
Task: "T031 [US3] Create document notifications in ContosoDashboard/Services/NotificationService.cs"
Task: "T032 [US3] Implement task-document attachment in ContosoDashboard/Services/TaskDocumentService.cs"
Task: "T039 [US3] Implement the Azure Queue-triggered scan function in AzureFunctions/DocumentScanFunction.cs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 shared foundation.
3. Complete Phase 3 User Story 1.
4. Validate upload, quarantine, scan, rejection, and authorized browsing independently.
5. Demonstrate the MVP before starting search and collaboration enhancements.

### Incremental Delivery

1. Add User Story 2 for search, preview, download, and document management.
2. Add User Story 3 for sharing, task/dashboard integration, notifications, and audit reports.
3. Add the Azure Queue Storage and Function deployment path without changing the offline training path.
4. Complete Phase 6 cross-cutting validation and documentation.

### Format Validation

Every task uses the required `- [ ] T###` checklist prefix, uses `[P]` only for parallelizable work, uses `[US1]`, `[US2]`, or `[US3]` on user-story tasks, and includes one or more exact file paths.
