using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM
{
    public class Collaboration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; } // User who created the collab

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys 
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
