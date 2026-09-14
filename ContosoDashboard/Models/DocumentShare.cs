using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }
    public int DocumentId { get; set; }
    public int? SharedWithUserId { get; set; }

    [MaxLength(100)]
    public string? SharedWithTeam { get; set; }

    public int SharedByUserId { get; set; }
    public DateTime SharedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;
}
