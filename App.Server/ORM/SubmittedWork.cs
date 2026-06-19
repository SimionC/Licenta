using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class SubmittedWork
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CourseWorkId { get; set; }

    [Required]
    public int StudentId { get; set; }

    public int? NoteId { get; set; }

    public int? NoteSnapshotId { get; set; }
    
    public int? GradeId { get; set; }

    public string? TextAnswer { get; set; }

    public string? OriginalFileName { get; set; }

    public string? StoredFileName { get; set; }

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Foreign keys
    [ForeignKey("CourseWorkId")]
    public CourseWork CourseWork { get; set; } = null!;

    [ForeignKey("StudentId")]
    public User Student { get; set; } = null!;
    
    [ForeignKey("NoteId")]
    public Note? Note { get; set; } = null;

    [ForeignKey("NoteSnapshotId")]
    public NoteSnapshot? NoteSnapshot { get; set; } = null;

    [ForeignKey("GradeId")]
    public Grade? Grade { get; set; } = null;
}
