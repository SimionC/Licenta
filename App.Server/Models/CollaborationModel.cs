using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace App.Server.Models
{
    public class CollaborationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string CreatedByUserId { get; set; } = string.Empty; // User who created the collab

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<CollaborationMemberModel> Members { get; set; } = new();
        public List<NoteModel> Notes { get; set; } = new();
    }
}
