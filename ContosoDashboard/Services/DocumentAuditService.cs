using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentAuditService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(int actorUserId, int? documentId, string action, string outcome, string? details = null)
    {
        _context.DocumentAuditEvents.Add(new DocumentAuditEvent
        {
            ActorUserId = actorUserId,
            DocumentId = documentId,
            Action = action,
            Outcome = outcome,
            Details = details,
            OccurredDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentAuditEvent>> GetReportAsync(int requestingUserId)
    {
        var isAdministrator = await _context.Users.AnyAsync(u => u.UserId == requestingUserId && u.Role == UserRole.Administrator);
        if (!isAdministrator) return [];
        return await _context.DocumentAuditEvents
            .OrderByDescending(a => a.OccurredDate)
            .Take(1000)
            .ToListAsync();
    }
}
