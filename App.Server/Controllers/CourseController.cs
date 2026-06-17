using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

//Purpose: Course lifecycle, role rules, resources, and coursework endpoints.
//Inputs/Outputs: Uses body DTOs, multipart files, and current user claims.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/Models/CourseModel.cs, App.Server/Models/CourseWorkDTO.cs.

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public CourseController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost("create")]
    public IActionResult CreateCourse([FromBody] CourseModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Description))
            return BadRequest("Title and Description are required.");

        var userTypeId = User.FindFirst("UserTypeId")?.Value;
        if (userTypeId != "2")
            return Forbid();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);
        if (user == null) return NotFound("User not found");

        var rand = new Random();
        string password;
        do
        {
            password = rand.Next(10, 100).ToString();
        }
        while (_context.Courses.Any(c => c.JoinPassword == password));

        var course = new Course
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            TeacherId = user.Id,
            JoinPassword = password,
            IsClosed = false
        };

        _context.Courses.Add(course);
        _context.SaveChanges();

        course.Teacher = user;
        return Ok(MapCourseDetail(course, isTeacherOwner: true, isEnrolled: false));
    }

    [HttpGet("all")]
    public IActionResult GetAllCourses()
    {
        var userId = GetCurrentUserId();
        var userTypeId = User.FindFirst("UserTypeId")?.Value;

        if (userId == null || userTypeId == null)
            return Unauthorized();

        if (userTypeId == "2")
        {
            var ownCourses = _context.Courses
                .Include(c => c.Teacher)
                .Where(c => c.TeacherId == userId.Value)
                .ToList();

            return Ok(ownCourses.Select(MapCourseSummary));
        }

        var joinedCourseIds = _context.UsersCourses
            .Where(uc => uc.UserId == userId.Value)
            .Select(uc => uc.CourseId);

        var joinedCourses = _context.Courses
            .Include(c => c.Teacher)
            .Where(c => joinedCourseIds.Contains(c.Id))
            .ToList();

        return Ok(joinedCourses.Select(MapCourseSummary));
    }

    [HttpDelete("delete/{id}")]
    public IActionResult DeleteCourse(int id)
    {
        var course = _context.Courses.Find(id);
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (course.TeacherId != userId.Value) return Forbid();

        _context.Courses.Remove(course);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPost("join")]
    public IActionResult JoinCourse([FromBody] string password)
    {
        if (User.FindFirst("UserTypeId")?.Value == "2")
            return BadRequest("Teachers cannot enroll in courses.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var course = _context.Courses.FirstOrDefault(c => c.JoinPassword == password);
        if (course == null) return NotFound("Invalid password");
        if (course.IsClosed) return BadRequest("This course is closed for enrollment.");

        var alreadyJoined = _context.UsersCourses.Any(uc =>
            uc.UserId == userId.Value && uc.CourseId == course.Id);

        if (!alreadyJoined)
        {
            _context.UsersCourses.Add(new UserCourse
            {
                UserId = userId.Value,
                CourseId = course.Id
            });

            _context.SaveChanges();
        }

        return Ok(course);
    }

    [HttpGet("student")]
    public IActionResult GetStudentCourses()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var registeredIds = _context.UsersCourses
            .Where(uc => uc.UserId == userId.Value)
            .Select(uc => uc.CourseId)
            .ToList();

        var registeredCourses = _context.Courses
            .Include(c => c.Teacher)
            .Where(c => registeredIds.Contains(c.Id))
            .ToList();

        var otherCourses = _context.Courses
            .Include(c => c.Teacher)
            .Where(c => !registeredIds.Contains(c.Id))
            .ToList();

        return Ok(new
        {
            registered = registeredCourses.Select(MapCourseSummary),
            others = otherCourses.Select(MapCourseSummary)
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetCourse(int id)
    {
        var course = _context.Courses
            .Include(c => c.Teacher)
            .FirstOrDefault(c => c.Id == id);

        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var isTeacherOwner = course.TeacherId == userId.Value;
        var isEnrolled = _context.UsersCourses.Any(uc => uc.UserId == userId.Value && uc.CourseId == id);

        if (!isTeacherOwner && !isEnrolled)
            return Forbid();

        return Ok(MapCourseDetail(course, isTeacherOwner, isEnrolled));
    }

    [HttpPut("{id}")]
    public IActionResult UpdateCourse(int id, [FromBody] CourseUpdateModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Description))
            return BadRequest("Title and Description are required.");

        var course = _context.Courses
            .Include(c => c.Teacher)
            .FirstOrDefault(c => c.Id == id);
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (course.TeacherId != userId.Value) return Forbid();

        course.Title = model.Title.Trim();
        course.Description = model.Description.Trim();
        _context.SaveChanges();

        return Ok(MapCourseDetail(course, isTeacherOwner: true, isEnrolled: false));
    }

    [HttpPost("{id}/close")]
    public IActionResult CloseCourse(int id)
    {
        return SetCourseClosedState(id, true);
    }

    [HttpPost("{id}/reopen")]
    public IActionResult ReopenCourse(int id)
    {
        return SetCourseClosedState(id, false);
    }

    [HttpPost("{courseId}/coursework")]
    public IActionResult CreateCourseWork(int courseId, [FromBody] CourseWorkDTO dto)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course == null)
            return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (course.TeacherId != userId.Value)
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

    [HttpGet("{courseId}/courseworks")]
    public IActionResult GetCourseWorksForCourse(int courseId)
    {
        if (!CanAccessCourse(courseId))
            return Forbid();

        var courseWorks = _context.CourseWork
            .Where(cw => cw.CourseId == courseId)
            .ToList();

        return Ok(courseWorks);
    }

    [HttpGet("{courseId}/resources")]
    public IActionResult GetResourcesForCourse(int courseId)
    {
        if (!CanAccessCourse(courseId))
            return Forbid();

        var resources = _context.CourseResources
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.UploadedAt)
            .ToList()
            .Select(MapResource);

        return Ok(resources);
    }

    [HttpPost("{courseId}/resources")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadResource(int courseId, IFormFile file)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (course.TeacherId != userId.Value) return Forbid();
        if (file.Length == 0) return BadRequest("Choose a non-empty file.");

        var uploadDirectory = GetResourceDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var resource = new CourseResource
        {
            CourseId = courseId,
            OriginalFileName = originalName,
            StoredFileName = storedName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _context.CourseResources.Add(resource);
        await _context.SaveChangesAsync();

        return Ok(MapResource(resource));
    }

    [HttpGet("resources/{resourceId}/download")]
    public async Task<IActionResult> DownloadResource(int resourceId)
    {
        var resource = await _context.CourseResources.FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        if (!CanAccessCourse(resource.CourseId))
            return Forbid();

        var fullPath = Path.Combine(GetResourceDirectory(), resource.StoredFileName);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing from storage.");

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, resource.ContentType, resource.OriginalFileName);
    }

    [HttpDelete("resources/{resourceId}")]
    public async Task<IActionResult> DeleteResource(int resourceId)
    {
        var resource = await _context.CourseResources
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (resource.Course.TeacherId != userId.Value) return Forbid();

        var fullPath = Path.Combine(GetResourceDirectory(), resource.StoredFileName);
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.CourseResources.Remove(resource);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IActionResult SetCourseClosedState(int id, bool isClosed)
    {
        var course = _context.Courses
            .Include(c => c.Teacher)
            .FirstOrDefault(c => c.Id == id);
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (course.TeacherId != userId.Value) return Forbid();

        course.IsClosed = isClosed;
        _context.SaveChanges();

        return Ok(MapCourseDetail(course, isTeacherOwner: true, isEnrolled: false));
    }

    private bool CanAccessCourse(int courseId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return false;

        return _context.Courses.Any(c => c.Id == courseId && c.TeacherId == userId.Value)
            || _context.UsersCourses.Any(uc => uc.CourseId == courseId && uc.UserId == userId.Value);
    }

    private int? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = User.FindFirst("userId") ??
                          User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst("UserId");

        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private string GetResourceDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "CourseResources");
    }

    private static object MapCourseSummary(Course course)
    {
        return new
        {
            course.Id,
            course.Title,
            course.Description,
            course.IsClosed,
            TeacherName = $"{course.Teacher.Nume} {course.Teacher.Prenume}".Trim()
        };
    }

    private static object MapCourseDetail(Course course, bool isTeacherOwner, bool isEnrolled)
    {
        return new
        {
            course.Id,
            course.Title,
            course.Description,
            course.IsClosed,
            JoinPassword = isTeacherOwner ? course.JoinPassword : null,
            TeacherName = $"{course.Teacher.Nume} {course.Teacher.Prenume}".Trim(),
            Role = isTeacherOwner ? "teacher" : "student",
            CanManage = isTeacherOwner,
            IsEnrolled = isEnrolled
        };
    }

    private static object MapResource(CourseResource resource)
    {
        return new
        {
            resource.Id,
            Title = resource.OriginalFileName,
            FileName = resource.OriginalFileName,
            Type = Path.GetExtension(resource.OriginalFileName).TrimStart('.').ToUpperInvariant(),
            resource.Size,
            resource.UploadedAt
        };
    }
}

public class CourseUpdateModel
{
    public required string Title { get; set; }
    public required string Description { get; set; }
}
