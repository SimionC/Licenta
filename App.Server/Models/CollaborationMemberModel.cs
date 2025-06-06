using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.Models
{
    public class CollaborationMemberModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CollaborationId { get; set; }

        [ForeignKey("CollaborationId")]
        public CollaborationModel Collaboration { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty; // The user who is a member

        // Example roles: "owner", "editor", "viewer"
        public string Role { get; set; } = "editor";
    }
}
