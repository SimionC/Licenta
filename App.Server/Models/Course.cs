using System.ComponentModel.DataAnnotations;

namespace App.Server.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public string TeacherEmail { get; set; } = "";
    }
}
