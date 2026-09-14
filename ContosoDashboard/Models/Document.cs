using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = "Other";

    [MaxLength(1000)]
    public string? Tags { get; set; }

    [Required, MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = DocumentStatuses.PendingScan;

    [Required]
    public int UploaderId { get; set; }

    public int? ProjectId { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ScannedDate { get; set; }

    [ForeignKey(nameof(UploaderId))]
    public virtual User Uploader { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<TaskDocument> TaskLinks { get; set; } = new List<TaskDocument>();
    public virtual ICollection<DocumentAuditEvent> AuditEvents { get; set; } = new List<DocumentAuditEvent>();
}

public static class DocumentStatuses
{
    public const string PendingScan = "PendingScan";
    public const string Available = "Available";
    public const string Rejected = "Rejected";
    public const string Deleted = "Deleted";
}

public static class DocumentCategories
{
    public static readonly string[] All =
    [
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    ];
}
