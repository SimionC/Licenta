using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class NoteSnapshot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SourceNoteId { get; set; }

    [Required]
    public string SourceNoteGuid { get; set; } = null!;

    [Required]
    public int CreatedByUserId { get; set; }

    public string? TitleSnapshot { get; set; }

    public string ContentSnapshot { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string SnapshotType { get; set; } = "assignment_submission";

    [ForeignKey("SourceNoteId")]
    public Note SourceNote { get; set; } = null!;

    [ForeignKey("CreatedByUserId")]
    public User CreatedByUser { get; set; } = null!;
}
