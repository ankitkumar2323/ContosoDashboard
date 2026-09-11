<!--
Sync Impact Report
- Version change: 0.0.0 → 1.0.0
- Modified principles: N/A → Security-First Access Control, Quality-Driven Delivery,
  Performance & Resilience, Technical Standards & Compatibility,
  Offline-First, Replaceable Architecture
- Added sections: Security Requirements, Quality Standards, Performance Standards,
  Technical Standards, Development Workflow
- Removed sections: N/A
- Templates requiring updates: .specify/templates/plan-template.md ✅ reviewed (no changes required),
  .specify/templates/spec-template.md ✅ reviewed (no changes required),
  .specify/templates/tasks-template.md ✅ reviewed (no changes required),
  .specify/templates/commands/ ⚠ pending (directory not present in repository snapshot)
- Follow-up TODOs: None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security-First Access Control
All user-facing features MUST enforce authentication, role checks, authorization, and user isolation before data is displayed or modified. This project demonstrates secure-by-default patterns for Blazor Server, even in a training context. Any page, service method, or data query that exposes project or task data MUST verify the caller is authenticated and authorized to access the requested record; no default trust of query strings, route values, or UI state is allowed.

### II. Quality-Driven Delivery
This project MUST favor readable, maintainable, and testable code over clever or overly abstract implementations. Features must be small, explicit, and easy to review. Complexity is allowed only when justified by a documented requirement, and it must be balanced with clear naming, consistent layering, and low duplication.

### III. Performance & Resilience
The application MUST remain responsive for the expected training workload and must avoid unnecessary database, render, or authorization overhead. Expensive operations must be justified, bounded, and reviewed. When a feature introduces latency, blocking work, or repeated queries, the tradeoff must be documented and supported by verification.

### IV. Technical Standards & Compatibility
The repository MUST follow consistent .NET, C#, and Blazor conventions across services, pages, models, and configuration. New dependencies, patterns, or platform choices must be justified against the project’s offline-first and training-focused goals and must remain compatible with the supported framework version.

### V. Offline-First, Replaceable Architecture
Architecture MUST prioritize local, offline execution and clear abstractions for infrastructure swaps. Database, file storage, and identity providers must be separated behind service boundaries with explicit extension points. The system MUST be runnable without cloud dependencies, and migration paths to Azure or Microsoft Entra must remain documented and non-invasive to business logic.

## Security Requirements

The repository MUST maintain security guardrails appropriate to a training application that demonstrates real authorization patterns. The following requirements are non-negotiable:

- Authentication and authorization are mandatory for all protected pages and service operations.
- Cookie-based sessions require secure configuration, explicit logout handling, and controlled expiration.
- Data access checks MUST validate the authenticated user against project membership, role policy, and the requested resource.
- No credentials or secrets may be committed to the repository.
- Training sample credentials MAY be present only in documentation or seeded demo data and MUST be clearly labeled as mock or test-only.
- The application MUST detect and prevent direct object reference abuse and cross-user data leakage.

## Quality Standards

The project MUST maintain a quality bar that supports safe learning and predictable changes:

- Code reviews MUST check clarity, correctness, maintainability, and security impacts.
- Logic must be explicit and easy to trace from UI action to service behavior to persisted data.
- New or changed behavior MUST include verification evidence before merge.
- Duplication, dead code, and unnecessary complexity are not acceptable without documented justification.
- Errors and edge cases must be handled with deterministic behavior and actionable feedback.

## Performance Standards

The application MUST remain usable for the intended training scenarios without needless latency or repeated expensive operations:

- Query and page logic MUST avoid unnecessary data loads or repeated retrievals.
- Data access patterns MUST be efficient and scoped to the user’s actual needs.
- Performance-sensitive code MUST be evaluated against user-visible responsiveness, not only correctness.
- Regressions in page load time, rendering behavior, or service throughput must be treated as defects.
- Work that requires long-running or resource-heavy processing MUST be documented and justified.

## Technical Standards

The codebase MUST maintain a consistent, maintainable technical baseline:

- ASP.NET Core, C#, and Blazor conventions MUST be used consistently across the repository.
- Services, pages, models, and data access code MUST respect clear boundaries and dependency direction.
- Dependencies and framework versions MUST be kept compatible with the project’s supported environment.
- New patterns, libraries, or architectural changes MUST be justified in planning artifacts and reviewed against the project’s training scope.
- The repository MUST prefer explicit, discoverable implementations over hidden framework magic or implicit behavior.

## Development Workflow

All work in this repository MUST follow a reviewable, spec-driven workflow that keeps security, quality, performance, and technical consistency visible in every change:

- Features MUST be planned, specified, and reviewed before implementation.
- Each story MUST describe a user outcome, acceptance criteria, and a verification method.
- The repository MUST prefer small, reviewable changes; broad refactors are allowed only when their purpose is documented and required.
- Code reviews MUST confirm security, authorization, and user-isolation checks for every data path touched.
- The definition of done includes passing validation for the affected behavior and confirming there are no regressions in protected flows.
- Changes that affect authentication, authorization, data access, performance, or architecture require explicit verification evidence before merge.

## Governance

This Constitution supersedes ad hoc practices for this repository. All changes to behavior, security boundaries, or architecture must remain consistent with these principles. Implementation decisions that conflict with this document require a written justification and explicit approval before merge.

Amendments require:

1. A proposed change and rationale in the relevant spec or planning artifact.
2. A review of the impact on security, authorization, architecture, and training scope.
3. Approval from the project owner or designated maintainer before merge.
4. Update of the version number and the amendment date in this constitution.

Versioning policy:

- MAJOR: backward-incompatible changes to required principles, security guarantees, or governance requirements.
- MINOR: new principle, clarifying requirement, or materially expanded guidance.
- PATCH: wording, typo, or non-semantic clarification that does not change required behavior.

Compliance review:

- Reviewers MUST verify that new or changed code preserves authentication, authorization, and user isolation.
- Pull requests introducing data access changes MUST include a security check and a test or verification step.
- Any deviation from the offline-first or training-scoped constraints MUST be documented with an approved rationale.

**Version**: 1.0.0 | **Ratified**: 2026-09-12 | **Last Amended**: 2026-09-12
