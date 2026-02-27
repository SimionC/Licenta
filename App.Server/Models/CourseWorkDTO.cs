// Purpose: input DTO for course assignment creation.
// Contract: title required, description optional, deadline optional.

namespace App.Server.Models
{
    public class CourseWorkDTO
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
    }

}
