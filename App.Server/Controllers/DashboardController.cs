using App.Server.ORM;
using App.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

//Purpose: the backend endpoint that gives data to DashboardPage.jsx
//Info: Uses current authenticated user; returns recent courses, recent notes, and assignments

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var roleId = GetCurrentUserTypeId();
        var isTeacher = roleId != null && UserRoles.IsTeacher(roleId.Value);
        var recentCourses = isTeacher
            ? await GetTeacherCourses(userId.Value)
            : await GetStudentCourses(userId.Value);

        var recentNotes = await GetRecentNotes(userId.Value);
        var urgentAssignments = isTeacher
            ? await GetTeacherAssignments(userId.Value)
            : await GetStudentAssignments(userId.Value);

        return Ok(new
        {
            Role = roleId == null ? UserRoles.StudentName : UserRoles.GetName(roleId.Value),
            RecentCourses = recentCourses,
            RecentNotes = recentNotes,
            UrgentAssignments = urgentAssignments
        });
    }

    private async Task<List<DashboardCourseItem>> GetStudentCourses(int userId)
    {
        return await _context.UsersCourses
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Course)
            .ThenInclude(c => c.Teacher)
            .GroupJoin(
                _context.CourseUserActivities.Where(a => a.UserId == userId),
                uc => uc.CourseId,
                activity => activity.CourseId,
                (uc, activities) => new { UserCourse = uc, Activity = activities.FirstOrDefault() })
            .OrderByDescending(item => item.Activity != null ? item.Activity.LastAccessedAt : DateTime.MinValue)
            .ThenByDescending(item => item.UserCourse.CourseId)
            .Take(4)
            .Select(item => new DashboardCourseItem
            {
                Id = item.UserCourse.Course.Id,
                Title = item.UserCourse.Course.Title,
                Description = item.UserCourse.Course.Description,
                IsClosed = item.UserCourse.Course.IsClosed,
                TeacherName = (item.UserCourse.Course.Teacher.Nume + " " + item.UserCourse.Course.Teacher.Prenume).Trim(),
                LastAccessedAt = item.Activity != null ? item.Activity.LastAccessedAt : null
            })
            .ToListAsync();
    }

    private async Task<List<DashboardCourseItem>> GetTeacherCourses(int userId)
    {
        return await _context.Courses
            .Include(c => c.Teacher)
            .Where(c => c.TeacherId == userId)
            .GroupJoin(
                _context.CourseUserActivities.Where(a => a.UserId == userId),
                c => c.Id,
                activity => activity.CourseId,
                (course, activities) => new { Course = course, Activity = activities.FirstOrDefault() })
            .OrderByDescending(item => item.Activity != null ? item.Activity.LastAccessedAt : DateTime.MinValue)
            .ThenByDescending(item => item.Course.Id)
            .Take(4)
            .Select(item => new DashboardCourseItem
            {
                Id = item.Course.Id,
                Title = item.Course.Title,
                Description = item.Course.Description,
                IsClosed = item.Course.IsClosed,
                TeacherName = (item.Course.Teacher.Nume + " " + item.Course.Teacher.Prenume).Trim(),
                LastAccessedAt = item.Activity != null ? item.Activity.LastAccessedAt : null
            })
            .ToListAsync();
    }

    private async Task<List<DashboardNoteItem>> GetRecentNotes(int userId)
    {
        var collaborationIds = await _context.CollaborationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.CollaborationId)
            .ToListAsync();

        var sharedNoteIds = await _context.NotePermissions
            .Where(p => p.UserId == userId && p.Status == "accepted")
            .Select(p => p.NoteId)
            .ToListAsync();

        return await _context.Notes
            .Include(n => n.Folder)
            .Include(n => n.Collaboration)
            .Where(n =>
                (n.UserId == userId && n.CollaborationId == null) ||
                (n.CollaborationId != null && collaborationIds.Contains(n.CollaborationId.Value)) ||
                (n.CollaborationId == null && sharedNoteIds.Contains(n.Id)))
            .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
            .Take(4)
            .Select(n => new DashboardNoteItem
            {
                Id = n.Id,
                Guid = n.Guid,
                Title = n.Title,
                Preview = n.Text.Length > 130 ? n.Text.Substring(0, 130) : n.Text,
                UpdatedAt = n.ModifyDate ?? n.CreationDate,
                FolderName = n.Folder != null ? n.Folder.Name : null,
                CollaborationId = n.CollaborationId,
                CollaborationName = n.Collaboration != null ? n.Collaboration.Name : null
            })
            .ToListAsync();
    }

    private async Task<List<DashboardAssignmentItem>> GetStudentAssignments(int userId)
    {
        var courseIds = await _context.UsersCourses
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CourseId)
            .ToListAsync();

        var submittedIds = await _context.SubmittedWork
            .Where(sw => sw.StudentId == userId)
            .Select(sw => sw.CourseWorkId)
            .ToListAsync();

        var assignments = await _context.CourseWork
            .Include(cw => cw.Course)
            .Where(cw => courseIds.Contains(cw.CourseId) && !submittedIds.Contains(cw.Id))
            .ToListAsync();

        return assignments
            .Where(cw => AssignmentIsStillOpen(cw.Deadline))
            .Select(cw =>
            {
                var urgency = GetStudentUrgency(cw.Deadline);
                return new DashboardAssignmentItem
                {
                    Id = cw.Id,
                    CourseId = cw.CourseId,
                    Title = cw.Title,
                    CourseTitle = cw.Course.Title,
                    Deadline = cw.Deadline,
                    Label = urgency.Label,
                    Status = urgency.Status,
                    SortOrder = urgency.SortOrder
                };
            })
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Deadline ?? DateTime.MaxValue)
            .ToList();
    }

    private async Task<List<DashboardAssignmentItem>> GetTeacherAssignments(int userId)
    {
        var courseIds = await _context.Courses
            .Where(c => c.TeacherId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var assignments = await _context.CourseWork
            .Include(cw => cw.Course)
            .Where(cw => courseIds.Contains(cw.CourseId))
            .ToListAsync();

        var ungradedCounts = await _context.SubmittedWork
            .Where(sw => courseIds.Contains(sw.CourseWork.CourseId))
            .GroupBy(sw => sw.CourseWorkId)
            .Select(g => new { CourseWorkId = g.Key, Count = g.Count(sw => sw.GradeId == null) })
            .ToDictionaryAsync(g => g.CourseWorkId, g => g.Count);

        return assignments
            .Where(cw => AssignmentIsStillOpen(cw.Deadline))
            .Select(cw =>
            {
                ungradedCounts.TryGetValue(cw.Id, out var ungraded);
                var urgency = GetTeacherUrgency(cw.Deadline, ungraded);
                return new DashboardAssignmentItem
                {
                    Id = cw.Id,
                    CourseId = cw.CourseId,
                    Title = cw.Title,
                    CourseTitle = cw.Course.Title,
                    Deadline = cw.Deadline,
                    Label = urgency.Label,
                    Status = urgency.Status,
                    SortOrder = urgency.SortOrder,
                    UngradedCount = ungraded
                };
            })
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Deadline ?? DateTime.MaxValue)
            .ToList();
    }

    private static (string Status, string Label, int SortOrder) GetStudentUrgency(DateTime? deadline)
    {
        if (deadline == null) return ("ok", "Upcoming", 3);

        var now = DateTime.Now;
        var due = EndOfDayIfDateOnly(deadline.Value);
        if (due <= now.AddDays(3)) return ("urgent", "Urgent", 0);
        if (due <= now.AddDays(10)) return ("warning", "Due soon", 1);
        return ("ok", "Upcoming", 3);
    }
    // I wanted to make them different, but gave up, it could be an improvement later
    private static (string Status, string Label, int SortOrder) GetTeacherUrgency(DateTime? deadline, int ungradedCount)
    {
        if (deadline == null) return ("ok", "Upcoming", 3);

        var now = DateTime.Now;
        var due = EndOfDayIfDateOnly(deadline.Value);
        if (due <= now.AddDays(3)) return ("urgent", "Urgent", 0);
        if (due <= now.AddDays(10)) return ("warning", "Due soon", 1);
        return ("ok", "Upcoming", 3);
    }

    private static DateTime EndOfDayIfDateOnly(DateTime value) //until 07.10.2026 23:59:59 
    {
        var localValue = value.Kind == DateTimeKind.Utc
            ? value.ToLocalTime()
            : value;

        return localValue.TimeOfDay == TimeSpan.Zero
            ? localValue.Date.AddDays(1).AddTicks(-1)
            : localValue;
    }

    private static bool AssignmentIsStillOpen(DateTime? deadline)
    {
        return deadline == null || EndOfDayIfDateOnly(deadline.Value) >= DateTime.Now;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetCurrentUserTypeId()
    {
        var claim = User.FindFirst("UserTypeId")?.Value;
        return int.TryParse(claim, out var roleId) ? roleId : null;
    }

    // Small response models - shape the data returned to the frontend -  not database tables
    public class DashboardCourseItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsClosed { get; set; }
        public string? TeacherName { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }

    public class DashboardNoteItem
    {
        public int Id { get; set; }
        public string Guid { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Preview { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? FolderName { get; set; }
        public int? CollaborationId { get; set; }
        public string? CollaborationName { get; set; }
    }

    public class DashboardAssignmentItem
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = "ok";
        public int SortOrder { get; set; }
        public int UngradedCount { get; set; }
    }
}
