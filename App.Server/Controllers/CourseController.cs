using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context)
    {
        _context = context;
    }

    // Create new course
    [HttpPost("create")]
    public IActionResult CreateCourse([FromBody] CourseModel course)
    {
        // generate unique 4-digit password
        var rand = new Random();
        string password;
        do
        { password = rand.Next(1000, 10000).ToString(); }
        while (_context.Courses.Any(c => c.JoinPassword == password));
        course.JoinPassword = password;

        _context.Courses.Add(course);
        _context.SaveChanges();
        return Ok(course);
    }

    // Get all courses (used on refresh)
    [HttpGet("all")]
    public IActionResult GetAllCourses()
    {
        return Ok(_context.Courses.ToList());
    }

    // Delete a course by ID
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteCourse(int id)
    {
        var course = _context.Courses.Find(id);
        if (course == null) return NotFound();

        _context.Courses.Remove(course);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPost("join")]
    public IActionResult JoinCourse([FromBody] string password)
    {
        var course = _context.Courses.FirstOrDefault(c => c.JoinPassword == password);
        if (course == null) return NotFound("Invalid password");

        // optionally check if already joined, etc.

        return Ok(course); // or just return course title/id
    }
}
