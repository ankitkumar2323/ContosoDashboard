using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IScanQueue _scanQueue;
    private readonly DocumentValidationService _validation;
    private readonly DocumentAuthorizationService _authorization;
    private readonly DocumentAuditService _audit;
    private readonly INotificationService _notifications;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IScanQueue scanQueue, DocumentValidationService validation, DocumentAuthorizationService authorization, DocumentAuditService audit, INotificationService notifications)
    {
        _context = context;
        _storage = storage;
        _scanQueue = scanQueue;
        _validation = validation;
        _authorization = authorization;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<DocumentUploadResult> UploadAsync(IFormFile file, string title, string? description, string category, string? tags, int? projectId, int userId, CancellationToken cancellationToken = default)
    {
        var validationError = _validation.Validate(file, category);
        if (validationError is not null) return new(false, file.FileName, null, validationError);
        if (projectId is not null && !await HasProjectAccessAsync(projectId.Value, userId))
            return new(false, file.FileName, null, "You are not authorized to upload to this project.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relativePath = Path.Combine(userId.ToString(), projectId?.ToString() ?? "personal", "quarantine", $"{Guid.NewGuid():N}{extension}");
        try
        {
            await _storage.SaveQuarantineAsync(file, relativePath, cancellationToken);
            var document = new Document
            {
                Title = title.Trim(),
                Description = description?.Trim(),
                Category = category,
                Tags = tags?.Trim(),
                FileName = Path.GetFileName(file.FileName),
                FilePath = relativePath,
                FileType = file.ContentType,
                FileSize = file.Length,
                Status = DocumentStatuses.PendingScan,
                UploaderId = userId,
                ProjectId = projectId
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            await _scanQueue.EnqueueAsync(new ScanQueueMessage(document.DocumentId, relativePath, Guid.NewGuid().ToString("N")), cancellationToken);
            await NotifyProjectMembersAsync(document, userId, "New project document", $"{document.Title} was added to the project.", NotificationType.DocumentAvailable);
            await _audit.RecordAsync(userId, document.DocumentId, "Upload", "PendingScan", file.FileName);
            return new(true, file.FileName, document.DocumentId, "Upload accepted and queued for scanning.");
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(userId, null, "Upload", "Failed", ex.Message);
            return new(false, file.FileName, null, "The file could not be stored.");
        }
    }

    public async Task<List<Document>> GetAuthorizedAsync(int userId, DocumentSearchFilters? filters = null)
    {
        filters ??= new DocumentSearchFilters();
        var query = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.Uploader)
            .Where(d => d.Status == DocumentStatuses.Available &&
                (d.UploaderId == userId ||
                 _context.Projects.Any(p => p.ProjectId == d.ProjectId && p.ProjectManagerId == userId) ||
                 _context.ProjectMembers.Any(pm => pm.ProjectId == d.ProjectId && pm.UserId == userId) ||
                 _context.DocumentShares.Any(s => s.DocumentId == d.DocumentId && s.SharedWithUserId == userId)));
        if (filters.ProjectId is not null) query = query.Where(d => d.ProjectId == filters.ProjectId);
        if (!string.IsNullOrWhiteSpace(filters.Category)) query = query.Where(d => d.Category == filters.Category);
        if (filters.From is not null) query = query.Where(d => d.UploadedDate >= filters.From);
        if (filters.To is not null) query = query.Where(d => d.UploadedDate <= filters.To);
        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            var term = filters.Query.Trim();
            query = query.Where(d => d.Title.Contains(term) || (d.Description != null && d.Description.Contains(term)) || (d.Tags != null && d.Tags.Contains(term)) || d.FileName.Contains(term));
        }
        return await query.OrderByDescending(d => d.UploadedDate).Take(500).ToListAsync();
    }

    public Task<Document?> GetByIdAsync(int documentId)
    {
        return _context.Documents
            .Include(d => d.Project)
            .Include(d => d.Uploader)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int userId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || document.Status == DocumentStatuses.Deleted || !await _authorization.CanManageAsync(document, userId)) return false;
        if (_validation.ValidateMetadata(title, category) is not null) return false;
        document.Title = title.Trim();
        document.Description = description?.Trim();
        document.Category = category;
        document.Tags = tags?.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(userId, documentId, "MetadataUpdate", "Succeeded");
        return true;
    }

    public async Task<bool> ReplaceAsync(int documentId, IFormFile file, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || !await _authorization.CanManageAsync(document, userId)) return false;
        if (_validation.Validate(file, document.Category) is not null) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var quarantinePath = Path.Combine(userId.ToString(), document.ProjectId?.ToString() ?? "personal", "quarantine", $"{Guid.NewGuid():N}{extension}");
        await _storage.SaveQuarantineAsync(file, quarantinePath, cancellationToken);
        var previousPath = document.Status == DocumentStatuses.Available ? document.FilePath : null;
        document.FileName = Path.GetFileName(file.FileName);
        document.FileType = file.ContentType;
        document.FileSize = file.Length;
        document.FilePath = quarantinePath;
        document.Status = DocumentStatuses.PendingScan;
        document.ScannedDate = null;
        await _context.SaveChangesAsync(cancellationToken);
        await _scanQueue.EnqueueAsync(new ScanQueueMessage(document.DocumentId, quarantinePath, Guid.NewGuid().ToString("N"), previousPath), cancellationToken);
        await _audit.RecordAsync(userId, documentId, "Replacement", "PendingScan", file.FileName);
        return true;
    }

    public async Task<DocumentShareResult> ShareAsync(int documentId, int recipientUserId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || !await _authorization.CanManageAsync(document, userId) || !await _authorization.CanReadAsync(document, recipientUserId))
            return new(false, "The document or recipient is not authorized.");
        if (!await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.SharedWithUserId == recipientUserId, cancellationToken))
        {
            _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, SharedWithUserId = recipientUserId, SharedByUserId = userId });
            await _context.SaveChangesAsync(cancellationToken);
            await _notifications.CreateNotificationAsync(new Notification { UserId = recipientUserId, Title = "Document shared with you", Message = $"{document.Title} was shared with you.", Type = NotificationType.DocumentShare, Priority = NotificationPriority.Important });
            await _audit.RecordAsync(userId, documentId, "Share", "Succeeded", $"Recipient: {recipientUserId}");
        }
        return new(true, "Document shared.");
    }

    public async Task<List<Document>> GetSharedWithMeAsync(int userId)
    {
        return await _context.Documents.AsNoTracking().Include(d => d.Uploader)
            .Where(d => d.Status == DocumentStatuses.Available && d.Shares.Any(s => s.SharedWithUserId == userId))
            .OrderByDescending(d => d.UploadedDate).Take(500).ToListAsync();
    }

    private async Task NotifyProjectMembersAsync(Document document, int actorId, string title, string message, NotificationType type)
    {
        if (document.ProjectId is null) return;
        var recipientIds = await _context.ProjectMembers.Where(pm => pm.ProjectId == document.ProjectId && pm.UserId != actorId).Select(pm => pm.UserId).ToListAsync();
        var managerId = await _context.Projects.Where(p => p.ProjectId == document.ProjectId).Select(p => p.ProjectManagerId).FirstOrDefaultAsync();
        if (managerId != 0 && managerId != actorId) recipientIds.Add(managerId);
        foreach (var recipientId in recipientIds.Distinct())
            await _notifications.CreateNotificationAsync(new Notification { UserId = recipientId, Title = title, Message = message, Type = type });
    }

    public Task<bool> CanReadAsync(Document document, int userId)
    {
        return _authorization.CanReadAsync(document, userId);
    }

    public async Task<Stream?> OpenReadAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        if (!await _authorization.CanReadAsync(document, userId)) return null;
        return await _storage.OpenReadAsync(document.FilePath, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document is null || !await _authorization.CanManageAsync(document, userId)) return false;
        await _storage.DeleteAsync(document.FilePath, cancellationToken);
        document.Status = DocumentStatuses.Deleted;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(userId, documentId, "Delete", "Succeeded");
        return true;
    }

    private Task<bool> HasProjectAccessAsync(int projectId, int userId)
    {
        return _context.Projects.AnyAsync(p => p.ProjectId == projectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)));
    }
}
