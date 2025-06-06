using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM
{
    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Users foreign key 
        public int TeacherId { get; set; }
        
        public string JoinPassword { get; set; } = string.Empty;

        // Foreign key
        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; } = null!;
    }
}
