using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.Models
{
    public class NotePermissionModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NoteId { get; set; }

        [ForeignKey("NoteId")]
        public NoteModel Note { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty; // The user who gets the permission

        public bool CanEdit { get; set; } = false;
        public bool CanRead { get; set; } = true;
    }
}
