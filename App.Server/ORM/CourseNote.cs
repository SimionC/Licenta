using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class CourseNote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int NoteId { get; set; }

    [Required]
    public int CourseId { get; set; }

    // Foreign keys 
    [ForeignKey("NoteId")]
    public Note Note { get; set; } = null!;

    [ForeignKey("CourseId")]
    public Course Course { get; set; } = null!;
}
