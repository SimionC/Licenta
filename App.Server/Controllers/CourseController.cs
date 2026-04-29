using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

//Purpose: Course lifecycle and coursework endpoints.
//Inputs/Outputs: Uses body DTOs plus current user claims; returns course/coursework lists and created entities.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/Models/CourseModel.cs, App.Server/Models/CourseWorkDTO.cs.

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context)
    {
        _context = context;
    }


    //Trigger: POST create.
    //Guards: Validates title/description; requires Email claim; must be teacher (UserTypeId == "2").
    //Actions: Finds teacher user, generates join password, inserts course.
    //Result: Returns created course.
    [HttpPost("create")]
    public IActionResult CreateCourse([FromBody] CourseModel model)
    {
        // Validate input (optional, but good practice)
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Description))
            return BadRequest("Title and Description are required.");

        // Save the email of the teacher (logged in user)
        var email = User.FindFirst("Email")?.Value;
        var userTypeId = User.FindFirst("UserTypeId")?.Value;
        if (email == null) return Unauthorized();
        
        // Check if user is a teacher (UserTypeId == "2")
        if (userTypeId != "2")
            return Forbid();

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        // Generate unique 4-digit password
        var rand = new Random();
        string password;
        do
        {
            password = rand.Next(10, 100).ToString();
        }
        while (_context.Courses.Any(c => c.JoinPassword == password));

        // Create the course entity
        var course = new Course
        {
            Title = model.Title,
            Description = model.Description,
            TeacherId = user.Id,
            JoinPassword = password
            // You can set other fields here as needed
        };

        _context.Courses.Add(course);
        _context.SaveChanges();
        return Ok(course);
    }


    //Trigger: GET all - courses (used on refresh).
    //Guards: Requires Email/UserTypeId/userId claims.
    //Actions: If teacher, filters by TeacherId; otherwise returns all.
    //Result: Role-specific course list.
    [HttpGet("all")]
    public IActionResult GetAllCourses()
    {
        var email = User.FindFirst("Email")?.Value;
        var userIdStr = User.FindFirst("userId")?.Value;
        var userTypeId = User.FindFirst("UserTypeId")?.Value;

        if (email == null || userTypeId == null || userIdStr == null)
            return Unauthorized();

        var userId = int.Parse(userIdStr);

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



    //Trigger: DELETE course by id.
    //Guards: Course must exist; requester must be course owner (TeacherId).
    //Actions: Removes course row.
    //Result: 204 NoContent.
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteCourse(int id)
    {
        var course = _context.Courses.Find(id);
        if (course == null) return NotFound();
        
        // Verify requester is the course owner (teacher)
        var userIdStr = User.FindFirst("userId")?.Value;
        if (userIdStr == null || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();
        
        if (course.TeacherId != userId)
            return Forbid();

        _context.Courses.Remove(course);
        _context.SaveChanges();
        return NoContent();
    }


    //Trigger: POST join with password.
    //Guards: Requires auth user and valid password.
    //Actions: Ensures membership row exists in UsersCourses.
    //Result: Returns joined course.
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


    //Trigger: GET student - the courses for each student.
    //Guards: Requires authenticated user lookup.
    //Actions: Splits into registered and others.
    //Result: Returns object with both lists.
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


    //Trigger: GET by course id.
    //Guards: Not found returns 404.
    //Actions: Reads single course.
    //Result: Course payload.
    [HttpGet("{id}")]
    public IActionResult GetCourse(int id)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }


    //Trigger: POST courseId/createCoursework.
    //Guards: Course must exist; requester must be course owner (TeacherId).
    //Actions: Creates coursework row linked to course.
    //Result: Created coursework payload.
    [HttpPost("{courseId}/coursework")]
    public IActionResult CreateCourseWork(int courseId, [FromBody] CourseWorkDTO dto)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course == null)
            return NotFound();
        
        // Verify requester is the course owner (teacher)
        var userIdStr = User.FindFirst("userId")?.Value;
        if (userIdStr == null || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();
        
        if (course.TeacherId != userId)
            return Forbid();

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


    //Trigger: GET courseId/getCourseWork.
    //Guards: None beyond route validity.   
    //Actions: Filters coursework by CourseId.
    //Result: List of coursework items.
    [HttpGet("{courseId}/courseworks")]
    public IActionResult GetCourseWorksForCourse(int courseId)
    {
        var courseWorks = _context.CourseWork
            .Where(cw => cw.CourseId == courseId)
            .ToList();

        return Ok(courseWorks);
    }


}
