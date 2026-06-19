using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

//Purpose: Course lifecycle, role rules, resources, assignments, submissions, and grading endpoints.
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

        if (User.FindFirst("UserTypeId")?.Value != "2")
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
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (course.TeacherId != userId.Value) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

        var courseWork = new CourseWork
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Deadline = dto.Deadline,
            WeightPercent = NormalizeWeight(dto.WeightPercent),
            CourseId = courseId
        };

        _context.CourseWork.Add(courseWork);
        _context.SaveChanges();

        return Ok(MapCourseWork(courseWork, Array.Empty<CourseWorkResource>(), null));
    }

    [HttpPut("{courseId}/coursework/{courseWorkId}")]
    public IActionResult UpdateCourseWork(int courseId, int courseWorkId, [FromBody] CourseWorkDTO dto)
    {
        var courseWork = _context.CourseWork
            .Include(cw => cw.Course)
            .FirstOrDefault(cw => cw.Id == courseWorkId && cw.CourseId == courseId);
        if (courseWork == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (courseWork.Course.TeacherId != userId.Value) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

        courseWork.Title = dto.Title.Trim();
        courseWork.Description = dto.Description;
        courseWork.Deadline = dto.Deadline;
        courseWork.WeightPercent = NormalizeWeight(dto.WeightPercent);
        _context.SaveChanges();

        var resources = _context.CourseWorkResources
            .Where(r => r.CourseWorkId == courseWork.Id)
            .ToList();

        return Ok(MapCourseWork(courseWork, resources, null));
    }

    [HttpGet("{courseId}/courseworks")]
    public IActionResult GetCourseWorksForCourse(int courseId)
    {
        if (!CanAccessCourse(courseId))
            return Forbid();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var courseWorks = _context.CourseWork
            .Where(cw => cw.CourseId == courseId)
            .OrderBy(cw => cw.Deadline)
            .ToList();

        var courseWorkIds = courseWorks.Select(cw => cw.Id).ToList();
        var resourcesByCourseWork = _context.CourseWorkResources
            .Where(r => courseWorkIds.Contains(r.CourseWorkId))
            .ToList()
            .GroupBy(r => r.CourseWorkId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        var submissions = _context.SubmittedWork
            .Include(sw => sw.Grade)
            .Where(sw => sw.StudentId == userId.Value && courseWorkIds.Contains(sw.CourseWorkId))
            .ToDictionary(sw => sw.CourseWorkId);

        return Ok(courseWorks.Select(cw =>
            MapCourseWork(
                cw,
                resourcesByCourseWork.GetValueOrDefault(cw.Id, Array.Empty<CourseWorkResource>()),
                submissions.GetValueOrDefault(cw.Id))));
    }

    [HttpPost("coursework/{courseWorkId}/supporting-files")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadCourseWorkResource(int courseWorkId, IFormFile file)
    {
        var courseWork = await _context.CourseWork
            .Include(cw => cw.Course)
            .FirstOrDefaultAsync(cw => cw.Id == courseWorkId);
        if (courseWork == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (courseWork.Course.TeacherId != userId.Value) return Forbid();
        if (file.Length == 0) return BadRequest("Choose a non-empty file.");

        var resource = await SaveCourseWorkResource(courseWorkId, file);
        _context.CourseWorkResources.Add(resource);
        await _context.SaveChangesAsync();

        return Ok(MapCourseWorkResource(resource));
    }

    [HttpGet("coursework-files/{resourceId}/download")]
    public async Task<IActionResult> DownloadCourseWorkResource(int resourceId)
    {
        var resource = await _context.CourseWorkResources
            .Include(r => r.CourseWork)
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        if (!CanAccessCourse(resource.CourseWork.CourseId))
            return Forbid();

        var fullPath = Path.Combine(GetCourseWorkResourceDirectory(), resource.StoredFileName);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing from storage.");

        return File(System.IO.File.OpenRead(fullPath), resource.ContentType, resource.OriginalFileName);
    }

    [HttpDelete("coursework-files/{resourceId}")]
    public async Task<IActionResult> DeleteCourseWorkResource(int resourceId)
    {
        var resource = await _context.CourseWorkResources
            .Include(r => r.CourseWork)
            .ThenInclude(cw => cw.Course)
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (resource.CourseWork.Course.TeacherId != userId.Value) return Forbid();

        var fullPath = Path.Combine(GetCourseWorkResourceDirectory(), resource.StoredFileName);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

        _context.CourseWorkResources.Remove(resource);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("coursework/{courseWorkId}/submission")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpsertSubmission(int courseWorkId, [FromForm] string? textAnswer, IFormFile? file)
    {
        var courseWork = await _context.CourseWork
            .Include(cw => cw.Course)
            .FirstOrDefaultAsync(cw => cw.Id == courseWorkId);
        if (courseWork == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (courseWork.Course.TeacherId == userId.Value) return BadRequest("Teachers cannot submit assignments.");
        if (!IsStudentEnrolled(courseWork.CourseId, userId.Value)) return Forbid();
        if (DeadlineHasPassed(courseWork.Deadline)) return BadRequest("The deadline has passed.");
        if (string.IsNullOrWhiteSpace(textAnswer) && file == null)
            return BadRequest("Add a text answer or upload a file.");

        var submission = await _context.SubmittedWork
            .Include(sw => sw.Grade)
            .FirstOrDefaultAsync(sw => sw.CourseWorkId == courseWorkId && sw.StudentId == userId.Value);

        if (submission == null)
        {
            submission = new SubmittedWork
            {
                CourseWorkId = courseWorkId,
                StudentId = userId.Value,
                SubmittedAt = DateTime.UtcNow
            };
            _context.SubmittedWork.Add(submission);
        }

        submission.TextAnswer = textAnswer?.Trim();
        submission.NoteId = null;
        submission.NoteSnapshotId = null;
        submission.UpdatedAt = DateTime.UtcNow;

        if (file != null)
            await ReplaceSubmissionFile(submission, file);

        await _context.SaveChangesAsync();
        return Ok(MapSubmission(submission));
    }

    [HttpPost("coursework/{courseWorkId}/submit-note")]
    public async Task<IActionResult> SubmitNoteSnapshot(int courseWorkId, [FromBody] SubmitNoteSnapshotModel model)
    {
        return await SubmitNoteSnapshotInternal(courseWorkId, model);
    }

    [HttpPost("/api/Assignments/{assignmentId}/submit-note")]
    public async Task<IActionResult> SubmitNoteSnapshotByAssignmentRoute(int assignmentId, [FromBody] SubmitNoteSnapshotModel model)
    {
        return await SubmitNoteSnapshotInternal(assignmentId, model);
    }

    private async Task<IActionResult> SubmitNoteSnapshotInternal(int courseWorkId, SubmitNoteSnapshotModel model)
    {
        var courseWork = await _context.CourseWork
            .Include(cw => cw.Course)
            .FirstOrDefaultAsync(cw => cw.Id == courseWorkId);
        if (courseWork == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (courseWork.Course.TeacherId == userId.Value) return BadRequest("Teachers cannot submit assignments.");
        if (!IsStudentEnrolled(courseWork.CourseId, userId.Value)) return Forbid();
        if (DeadlineHasPassed(courseWork.Deadline)) return BadRequest("The deadline has passed.");

        var noteGuid = model.NoteGuid?.Trim();
        if (string.IsNullOrWhiteSpace(noteGuid))
            return BadRequest("A noteGuid is required.");

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == noteGuid);
        if (note == null) return NotFound("Note not found.");

        if (!await CanAccessNoteForSnapshot(note, userId.Value))
            return Forbid();

        var now = DateTime.UtcNow;
        var snapshot = new NoteSnapshot
        {
            SourceNoteId = note.Id,
            SourceNoteGuid = note.Guid,
            CreatedByUserId = userId.Value,
            TitleSnapshot = note.Title,
            ContentSnapshot = note.Text ?? string.Empty,
            CreatedAt = now,
            SnapshotType = "assignment_submission"
        };

        var submission = await _context.SubmittedWork
            .Include(sw => sw.Grade)
            .FirstOrDefaultAsync(sw => sw.CourseWorkId == courseWorkId && sw.StudentId == userId.Value);

        if (submission == null)
        {
            submission = new SubmittedWork
            {
                CourseWorkId = courseWorkId,
                StudentId = userId.Value,
                SubmittedAt = now
            };
            _context.SubmittedWork.Add(submission);
        }

        _context.NoteSnapshots.Add(snapshot);
        submission.NoteId = note.Id;
        submission.NoteSnapshot = snapshot;
        submission.TextAnswer = snapshot.ContentSnapshot;
        submission.UpdatedAt = now;

        await _context.SaveChangesAsync();
        return Ok(MapSubmission(submission));
    }

    [HttpGet("{courseId}/coursework/{courseWorkId}/submissions")]
    public async Task<IActionResult> GetSubmissionsForCourseWork(int courseId, int courseWorkId)
    {
        var courseWork = await _context.CourseWork
            .Include(cw => cw.Course)
            .FirstOrDefaultAsync(cw => cw.Id == courseWorkId && cw.CourseId == courseId);
        if (courseWork == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (courseWork.Course.TeacherId != userId.Value) return Forbid();

        var registeredStudents = await _context.UsersCourses
            .Where(uc => uc.CourseId == courseId)
            .Include(uc => uc.User)
            .Select(uc => uc.User)
            .OrderBy(u => u.Nume)
            .ThenBy(u => u.Prenume)
            .ToListAsync();

        var submissions = await _context.SubmittedWork
            .Include(sw => sw.Grade)
            .Include(sw => sw.NoteSnapshot)
            .Where(sw => sw.CourseWorkId == courseWorkId)
            .ToListAsync();

        return Ok(registeredStudents.Select(student =>
        {
            var submission = submissions.FirstOrDefault(sw => sw.StudentId == student.Id);
            return new
            {
                StudentId = student.Id,
                StudentName = $"{student.Nume} {student.Prenume}".Trim(),
                student.Email,
                HasSubmission = submission != null,
                Submission = submission == null ? null : MapSubmission(submission),
                Status = submission == null
                    ? (DeadlineHasPassed(courseWork.Deadline) ? "expired" : "pending")
                    : "completed"
            };
        }));
    }

    [HttpPost("submissions/{submissionId}/grade")]
    public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionModel model)
    {
        var submission = await _context.SubmittedWork
            .Include(sw => sw.CourseWork)
            .ThenInclude(cw => cw.Course)
            .Include(sw => sw.Grade)
            .Include(sw => sw.NoteSnapshot)
            .FirstOrDefaultAsync(sw => sw.Id == submissionId);
        if (submission == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (submission.CourseWork.Course.TeacherId != userId.Value) return Forbid();

        var gradeValue = Math.Clamp(model.GivenGrade, 0, 10);

        if (submission.Grade == null)
        {
            submission.Grade = new Grade();
            _context.Grades.Add(submission.Grade);
        }

        submission.Grade.GivenGrade = gradeValue;
        submission.Grade.Description = model.Comment;
        submission.Grade.GradedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapSubmission(submission));
    }

    [HttpGet("submissions/{submissionId}/download")]
    public async Task<IActionResult> DownloadSubmissionFile(int submissionId)
    {
        var submission = await _context.SubmittedWork
            .Include(sw => sw.CourseWork)
            .ThenInclude(cw => cw.Course)
            .FirstOrDefaultAsync(sw => sw.Id == submissionId);
        if (submission == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var canAccess = submission.StudentId == userId.Value ||
                        submission.CourseWork.Course.TeacherId == userId.Value;
        if (!canAccess) return Forbid();
        if (string.IsNullOrWhiteSpace(submission.StoredFileName) ||
            string.IsNullOrWhiteSpace(submission.OriginalFileName))
            return NotFound("No file was uploaded.");

        var fullPath = Path.Combine(GetSubmissionDirectory(), submission.StoredFileName);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing from storage.");

        return File(
            System.IO.File.OpenRead(fullPath),
            submission.ContentType ?? "application/octet-stream",
            submission.OriginalFileName);
    }

    [HttpGet("{courseId}/grades")]
    public async Task<IActionResult> GetGradesForCourse(int courseId)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var canManage = course.TeacherId == userId.Value;
        if (!canManage && !IsStudentEnrolled(courseId, userId.Value)) return Forbid();

        var assignments = await _context.CourseWork
            .Where(cw => cw.CourseId == courseId)
            .OrderBy(cw => cw.Deadline)
            .ToListAsync();

        if (!canManage)
        {
            var submissions = await _context.SubmittedWork
                .Include(sw => sw.Grade)
                .Include(sw => sw.NoteSnapshot)
                .Where(sw => sw.StudentId == userId.Value && assignments.Select(a => a.Id).Contains(sw.CourseWorkId))
                .ToListAsync();

            var rows = assignments.Select(assignment =>
            {
                var submission = submissions.FirstOrDefault(sw => sw.CourseWorkId == assignment.Id);
                return MapGradeRow(assignment, submission);
            }).ToList();

            return Ok(new
            {
                Role = "student",
                FinalGrade = CalculateFinalGrade(rows.Select(r =>
                    (r.Grade, assignments.First(a => a.Id == r.AssignmentId).WeightPercent))),
                Assignments = rows
            });
        }

        var students = await _context.UsersCourses
            .Where(uc => uc.CourseId == courseId)
            .Include(uc => uc.User)
            .Select(uc => uc.User)
            .OrderBy(u => u.Nume)
            .ThenBy(u => u.Prenume)
            .ToListAsync();

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var allSubmissions = await _context.SubmittedWork
            .Include(sw => sw.Grade)
            .Include(sw => sw.NoteSnapshot)
            .Where(sw => assignmentIds.Contains(sw.CourseWorkId))
            .ToListAsync();

        return Ok(new
        {
            Role = "teacher",
            Students = students.Select(student =>
            {
                var rows = assignments.Select(assignment =>
                {
                    var submission = allSubmissions.FirstOrDefault(sw =>
                        sw.StudentId == student.Id && sw.CourseWorkId == assignment.Id);
                    return MapGradeRow(assignment, submission);
                }).ToList();

                return new
                {
                    StudentId = student.Id,
                    StudentName = $"{student.Nume} {student.Prenume}".Trim(),
                    student.Email,
                    FinalGrade = CalculateFinalGrade(rows.Select(r =>
                        (r.Grade, assignments.First(a => a.Id == r.AssignmentId).WeightPercent))),
                    Assignments = rows
                };
            })
        });
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

    private bool IsStudentEnrolled(int courseId, int userId)
    {
        return _context.UsersCourses.Any(uc => uc.CourseId == courseId && uc.UserId == userId);
    }

    private async Task<bool> CanAccessNoteForSnapshot(Note note, int userId)
    {
        if (note.UserId == userId)
        {
            return true;
        }

        if (note.CollaborationId != null)
        {
            return await _context.CollaborationMembers.AnyAsync(cm =>
                cm.CollaborationId == note.CollaborationId.Value &&
                cm.UserId == userId);
        }

        return await _context.NotePermissions.AnyAsync(p =>
            p.NoteId == note.Id &&
            p.UserId == userId &&
            p.Status == "accepted");
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

    private string GetCourseWorkResourceDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "CourseWorkResources");
    }

    private string GetSubmissionDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "SubmittedWorkFiles");
    }

    private async Task<CourseWorkResource> SaveCourseWorkResource(int courseWorkId, IFormFile file)
    {
        var uploadDirectory = GetCourseWorkResourceDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var originalName = Path.GetFileName(file.FileName);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}";
        var fullPath = Path.Combine(uploadDirectory, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return new CourseWorkResource
        {
            CourseWorkId = courseWorkId,
            OriginalFileName = originalName,
            StoredFileName = storedName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };
    }

    private async Task ReplaceSubmissionFile(SubmittedWork submission, IFormFile file)
    {
        var uploadDirectory = GetSubmissionDirectory();
        Directory.CreateDirectory(uploadDirectory);

        if (!string.IsNullOrWhiteSpace(submission.StoredFileName))
        {
            var oldPath = Path.Combine(uploadDirectory, submission.StoredFileName);
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        var originalName = Path.GetFileName(file.FileName);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}";
        var fullPath = Path.Combine(uploadDirectory, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        submission.OriginalFileName = originalName;
        submission.StoredFileName = storedName;
        submission.ContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        submission.FileSize = file.Length;
    }

    private static bool DeadlineHasPassed(DateTime? deadline)
    {
        if (deadline == null) return false;

        var effectiveDeadline = deadline.Value;
        if (effectiveDeadline.TimeOfDay == TimeSpan.Zero)
            effectiveDeadline = effectiveDeadline.Date.AddDays(1).AddTicks(-1);

        if (effectiveDeadline.Kind == DateTimeKind.Unspecified)
            effectiveDeadline = DateTime.SpecifyKind(effectiveDeadline, DateTimeKind.Local);

        return DateTime.UtcNow > effectiveDeadline.ToUniversalTime();
    }

    private static decimal NormalizeWeight(decimal value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static decimal? CalculateFinalGrade(IEnumerable<(decimal? Grade, decimal Weight)> grades)
    {
        decimal total = 0;
        decimal totalWeight = 0;

        foreach (var (grade, weight) in grades)
        {
            if (grade == null || weight <= 0) continue;
            total += grade.Value * weight;
            totalWeight += weight;
        }

        if (totalWeight == 0) return null;
        return Math.Round(total / 100, 2);
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

    private static object MapCourseWork(
        CourseWork courseWork,
        IEnumerable<CourseWorkResource> resources,
        SubmittedWork? submission)
    {
        return new
        {
            courseWork.Id,
            courseWork.Title,
            courseWork.Description,
            courseWork.Deadline,
            courseWork.WeightPercent,
            Status = submission != null
                ? "completed"
                : DeadlineHasPassed(courseWork.Deadline) ? "expired" : "pending",
            SupportingFiles = resources.Select(MapCourseWorkResource),
            Submission = submission == null ? null : MapSubmission(submission)
        };
    }

    private static AssignmentGradeRow MapGradeRow(CourseWork assignment, SubmittedWork? submission)
    {
        return new AssignmentGradeRow
        {
            AssignmentId = assignment.Id,
            Title = assignment.Title,
            WeightPercent = assignment.WeightPercent,
            Status = submission != null
                ? "completed"
                : DeadlineHasPassed(assignment.Deadline) ? "expired" : "pending",
            SubmittedAt = submission?.SubmittedAt,
            Grade = submission?.Grade?.GivenGrade,
            Comment = submission?.Grade?.Description,
            IsGraded = submission?.Grade != null,
            MissingGrade = submission != null && submission.Grade == null
        };
    }

    private static object MapSubmission(SubmittedWork submission)
    {
        return new
        {
            submission.Id,
            submission.CourseWorkId,
            submission.StudentId,
            submission.TextAnswer,
            submission.OriginalFileName,
            submission.FileSize,
            submission.SubmittedAt,
            submission.UpdatedAt,
            NoteSnapshot = submission.NoteSnapshot == null
                ? null
                : new
                {
                    submission.NoteSnapshot.Id,
                    submission.NoteSnapshot.SourceNoteGuid,
                    Title = submission.NoteSnapshot.TitleSnapshot,
                    Content = submission.NoteSnapshot.ContentSnapshot,
                    submission.NoteSnapshot.CreatedAt,
                    submission.NoteSnapshot.SnapshotType
                },
            HasFile = !string.IsNullOrWhiteSpace(submission.StoredFileName),
            Grade = submission.Grade == null
                ? null
                : new
                {
                    submission.Grade.GivenGrade,
                    Comment = submission.Grade.Description,
                    submission.Grade.GradedAt
                }
        };
    }

    private static object MapCourseWorkResource(CourseWorkResource resource)
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

public class GradeSubmissionModel
{
    public decimal GivenGrade { get; set; }
    public string? Comment { get; set; }
}

public class SubmitNoteSnapshotModel
{
    public string? NoteGuid { get; set; }
}

public class AssignmentGradeRow
{
    public int AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal WeightPercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public decimal? Grade { get; set; }
    public string? Comment { get; set; }
    public bool IsGraded { get; set; }
    public bool MissingGrade { get; set; }
}
