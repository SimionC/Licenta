using App.Server.ORM;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public IActionResult CreateCourse([FromBody] Course course)
    {
        _context.Courses.Add(course);
        _context.SaveChanges();
        return Ok(course);
    }
}
