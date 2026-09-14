using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentAuditEvent
{
    [Key]
    public int DocumentAuditEventId { get; set; }
    public int? DocumentId { get; set; }
    public int ActorUserId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Outcome { get; set; } = string.Empty;

    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? Details { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }
}
