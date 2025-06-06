using App.Server.ORM;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM
{
    public class CollaborationMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CollaborationId { get; set; }

        [Required]
        public int UserId { get; set; } // The user who is a member

        // Example roles: "owner", "editor", "viewer"
        public string Role { get; set; } = "editor";


        // Foreign keys
        [ForeignKey("CollaborationId")]
        public Collaboration Collaboration { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // Assuming you have a UserModel for user details
    }
} 
