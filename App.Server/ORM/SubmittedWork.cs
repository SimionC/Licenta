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

    public int? NoteId { get; set; }
    
    public int? GradeId { get; set; }

    // Foreign keys
    [ForeignKey("CourseWorkId")]
    public CourseWork CourseWork { get; set; } = null!;
    
    [ForeignKey("NoteId")]
    public Note? Note { get; set; } = null;

    [ForeignKey("GradeId")]
    public Grade? Grade { get; set; } = null;
}
