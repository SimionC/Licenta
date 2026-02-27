// Purpose: input DTO for creating a course.
// Contract: title + description only; teacher identity comes from authenticated user claims.

namespace App.Server.Models
{
    public class CourseModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
