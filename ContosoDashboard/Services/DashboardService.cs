using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(int userId);
    Task<List<Announcement>> GetActiveAnnouncementsAsync();
    Task<List<Document>> GetRecentDocumentsAsync(int userId);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var summary = new DashboardSummary
        {
            TotalActiveTasks = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId && t.Status != Models.TaskStatus.Completed),

            TasksDueToday = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId 
                    && t.DueDate.HasValue 
                    && t.DueDate.Value.Date == now.Date
                    && t.Status != Models.TaskStatus.Completed),

            ActiveProjects = await _context.Projects
                .Where(p => p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId))
                .Where(p => p.Status == ProjectStatus.Active)
                .CountAsync(),

            UnreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead),

            DocumentCount = await _context.Documents.CountAsync(d => d.Status == DocumentStatuses.Available &&
                (d.UploaderId == userId || (d.ProjectId != null && (d.Project!.ProjectManagerId == userId || d.Project!.ProjectMembers.Any(pm => pm.UserId == userId))) || d.Shares.Any(s => s.SharedWithUserId == userId)))
        };

        return summary;
    }

    public async Task<List<Announcement>> GetActiveAnnouncementsAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Announcements
            .Include(a => a.CreatedByUser)
            .Where(a => a.IsActive 
                && a.PublishDate <= now 
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value > now))
            .OrderByDescending(a => a.PublishDate)
            .Take(5)
            .ToListAsync();
    }

    public Task<List<Document>> GetRecentDocumentsAsync(int userId)
    {
        return _context.Documents.AsNoTracking()
            .Where(d => d.Status == DocumentStatuses.Available &&
                (d.UploaderId == userId || (d.ProjectId != null && (d.Project!.ProjectManagerId == userId || d.Project!.ProjectMembers.Any(pm => pm.UserId == userId))) || d.Shares.Any(s => s.SharedWithUserId == userId)))
            .OrderByDescending(d => d.UploadedDate).Take(5).ToListAsync();
    }
}

public class DashboardSummary
{
    public int TotalActiveTasks { get; set; }
    public int TasksDueToday { get; set; }
    public int ActiveProjects { get; set; }
    public int UnreadNotifications { get; set; }
    public int DocumentCount { get; set; }
}
