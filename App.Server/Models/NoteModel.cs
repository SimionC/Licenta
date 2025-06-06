using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.Models
{
    public class NoteModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string OwnerId { get; set; } = string.Empty; // User ID (string or int depending on your User model)

        public bool IsPublic { get; set; } = false;

        public bool IsCode { get; set; } = false;

        public bool IsLatex { get; set; } = false;

        // Optional: link to Collaboration
        public int? CollaborationId { get; set; }
        [ForeignKey("CollaborationId")]
        public CollaborationModel? Collaboration { get; set; }

        // Navigation properties
        public List<NotePermissionModel> Permissions { get; set; } = new();
    }
}
