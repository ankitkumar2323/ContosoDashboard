# ContosoDashboard-SSD Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-09-14

## Active Technologies

- C# / .NET 10.0 + ASP.NET Core Blazor Server (006-document-management)
- Entity Framework Core 10.0 with SQLite metadata storage
- Local filesystem storage behind `IFileStorageService`; future Azure Blob implementation

## Project Structure

```text
ContosoDashboard/
├── Data/
├── Models/
├── Services/
├── Pages/
├── Shared/
└── wwwroot/
```

## Commands

cd ContosoDashboard; dotnet build; dotnet run

## Code Style

C# / .NET 10.0: Follow existing ASP.NET Core, Blazor, EF Core, dependency-injection, and service-layer authorization conventions.

## Recent Changes

- 006-document-management: Added local quarantine-first document storage, metadata search, protected file access, and audit design.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
