using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM
{
    public class NoteModel
    {
        public int Id { get; set; }

        public string? Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; } // User ID (string or int depending on your User model)

        public bool IsPublic { get; set; } = false;

        public int? CollaborationId { get; set; }

        public int? FolderId { get; set; }

        public string? FolderName { get; set; }

        // Added Guid property to match Note entity
        public string? Guid { get; set; }
    }
}
