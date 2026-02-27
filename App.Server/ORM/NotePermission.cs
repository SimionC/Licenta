using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// Purpose: per-user note permission table.
/// Current reality: collaboration-role logic is primarily enforced in controllers; 
///                  this table is not central yet.

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

        public bool CanEdit { get; set; } = false;
        public bool CanRead { get; set; } = true;

        // Foreign keys
        [ForeignKey("NoteId")]
        public Note Note { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
