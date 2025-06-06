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
    public IActionResult CreateCourse([FromBody] Course course)
    {
        //save the email of the teacher
        var email = User.FindFirst("Email")?.Value;
        if (email == null) return Unauthorized();

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound("User not found");
        
        course.TeacherId = user.Id;

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
        var email = User.FindFirst("Email")?.Value;
        var userId = int.Parse(User.FindFirst("userId")?.Value);
        var userTypeId = User.FindFirst("UserTypeId")?.Value;

        if (email == null || userTypeId == null)
            return Unauthorized();

        if (userTypeId == "2") // teacher
        {
            // Return only courses created by this teacher
            var ownCourses = _context.Courses
                .Where(c => c.TeacherId == userId)
                .ToList();

            return Ok(ownCourses);
        }

        // For students or other roles, return all courses
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
        var email = User.FindFirst("Email")?.Value;
        if (email == null) return Unauthorized();

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var course = _context.Courses.FirstOrDefault(c => c.JoinPassword == password);
        if (course == null) return NotFound("Invalid password");

        // Check if already registered
        bool alreadyJoined = _context.UsersCourses.Any(uc =>
            uc.UserId == user.Id && uc.CourseId == course.Id);

        if (!alreadyJoined)
        {
            _context.UsersCourses.Add(new UserCourse
            {
                UserId = user.Id,
                CourseId = course.Id
            });

            _context.SaveChanges();
        }

        return Ok(course);
    }


    [HttpGet("student")]
    public IActionResult GetStudentCourses()
    {
        var email = User.FindFirst("Email")?.Value;
        if (email == null) return Unauthorized();

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var registeredIds = _context.UsersCourses
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CourseId)
            .ToList();

        var registeredCourses = _context.Courses
            .Where(c => registeredIds.Contains(c.Id))
            .ToList();

        var otherCourses = _context.Courses
            .Where(c => !registeredIds.Contains(c.Id))
            .ToList();

        return Ok(new
        {
            registered = registeredCourses,
            others = otherCourses
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetCourse(int id)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpPost("{courseId}/coursework")]
    public IActionResult CreateCourseWork(int courseId, [FromBody] CourseWorkDTO dto)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course == null)
            return NotFound();

        var courseWork = new CourseWork
        {
            Title = dto.Title,
            Description = dto.Description,
            Deadline = dto.Deadline,
            CourseId = courseId
        };

        _context.CourseWork.Add(courseWork);
        _context.SaveChanges();

        return Ok(courseWork);
    }

    [HttpGet("{courseId}/courseworks")]
    public IActionResult GetCourseWorksForCourse(int courseId)
    {
        var courseWorks = _context.CourseWork
            .Where(cw => cw.CourseId == courseId)
            .ToList();

        return Ok(courseWorks);
    }


}
