using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class UserCourse
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CourseId { get; set; }

    // Foreign keys 
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public Course Course { get; set; } = null!;
}
