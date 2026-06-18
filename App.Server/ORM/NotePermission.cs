using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// Purpose: direct per-user sharing permissions for personal notes.

namespace App.Server.ORM
{
    public class NotePermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int NoteId { get; set; }

        [Required]
        public int UserId { get; set; } // The user who gets the permission

        [Required]
        public string Role { get; set; } = "viewer";

        [Required]
        public string Status { get; set; } = "accepted";

        [Required]
        public int InvitedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        [ForeignKey("NoteId")]
        public Note Note { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("InvitedByUserId")]
        public User InvitedByUser { get; set; } = null!;
    }
}
