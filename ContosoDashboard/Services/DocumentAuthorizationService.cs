using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanReadAsync(Document document, int userId)
    {
        if (document.Status != DocumentStatuses.Available) return false;
        if (document.UploaderId == userId) return true;
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRole.Administrator) return true;
        if (document.ProjectId is null) return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.SharedWithUserId == userId);
        return await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == document.ProjectId && pm.UserId == userId)
            || await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId)
            || await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.SharedWithUserId == userId);
    }

    public async Task<bool> CanManageAsync(Document document, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRole.Administrator || document.UploaderId == userId) return true;
        return document.ProjectId is not null && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId);
    }
}
