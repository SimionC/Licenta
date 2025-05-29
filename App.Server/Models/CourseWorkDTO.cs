namespace App.Server.Models
{
    public class CourseWorkDTO
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
    }

}
