using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class Note
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // For sharable links
    public string Guid { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateTime CreationDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int VisibilityTypeId { get; set; }

    public int? CollaborationId { get; set; }

    // Foreign keys
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("VisibilityTypeId")]
    public VisibilityType VisibilityType { get; set; } = null!;

    [ForeignKey("CollaborationId")]
    public Collaboration? Collaboration { get; set; } = null;
}


