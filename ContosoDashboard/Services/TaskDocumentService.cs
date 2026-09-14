using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class TaskDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentAuthorizationService _authorization;

    public TaskDocumentService(ApplicationDbContext context, DocumentAuthorizationService authorization)
    {
        _context = context;
        _authorization = authorization;
    }

    public async Task<bool> AttachAsync(int taskId, int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (task is null || document is null || task.ProjectId != document.ProjectId || !await _authorization.CanReadAsync(document, userId)) return false;
        var canAccessTask = task.AssignedUserId == userId || task.CreatedByUserId == userId || task.Project?.ProjectManagerId == userId || await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId, cancellationToken);
        if (!canAccessTask || await _context.TaskDocuments.AnyAsync(td => td.TaskId == taskId && td.DocumentId == documentId, cancellationToken)) return false;
        _context.TaskDocuments.Add(new TaskDocument { TaskId = taskId, DocumentId = documentId, AttachedByUserId = userId });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Document>> GetForTaskAsync(int taskId, int userId)
    {
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task is null) return [];
        var canAccessTask = task.AssignedUserId == userId || task.CreatedByUserId == userId || await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);
        if (!canAccessTask) return [];
        return await _context.TaskDocuments.Where(td => td.TaskId == taskId && td.Document.Status == DocumentStatuses.Available).Select(td => td.Document).AsNoTracking().ToListAsync();
    }
}