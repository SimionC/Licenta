using App.Server.ORM;
using App.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

//Purpose: Role-aware dashboard summary for existing dashboard cards.
//Inputs/Outputs: Uses current authenticated user; returns recent courses, recent notes, and urgent assignments.

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
            .OrderByDescending(uc => uc.CourseId)
            .Take(4)
            .Select(uc => new DashboardCourseItem
            {
                Id = uc.Course.Id,
                Title = uc.Course.Title,
                Description = uc.Course.Description,
                IsClosed = uc.Course.IsClosed,
                TeacherName = (uc.Course.Teacher.Nume + " " + uc.Course.Teacher.Prenume).Trim()
            })
            .ToListAsync();
    }

    private async Task<List<DashboardCourseItem>> GetTeacherCourses(int userId)
    {
        return await _context.Courses
            .Include(c => c.Teacher)
            .Where(c => c.TeacherId == userId)
            .OrderByDescending(c => c.Id)
            .Take(4)
            .Select(c => new DashboardCourseItem
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                IsClosed = c.IsClosed,
                TeacherName = (c.Teacher.Nume + " " + c.Teacher.Prenume).Trim()
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
            .Take(6)
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
            .Where(a => a.UngradedCount > 0 || a.SortOrder < 4)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Deadline ?? DateTime.MaxValue)
            .Take(6)
            .ToList();
    }

    private static (string Status, string Label, int SortOrder) GetStudentUrgency(DateTime? deadline)
    {
        if (deadline == null) return ("ok", "No deadline", 4);

        var now = DateTime.Now;
        var due = EndOfDayIfDateOnly(deadline.Value);
        if (due < now) return ("urgent", "Overdue", 0);
        if (due.Date == now.Date) return ("urgent", "Due today", 1);
        if (due <= now.AddDays(3)) return ("warning", "Due soon", 2);
        return ("ok", "Upcoming", 3);
    }

    private static (string Status, string Label, int SortOrder) GetTeacherUrgency(DateTime? deadline, int ungradedCount)
    {
        if (ungradedCount > 0) return ("urgent", $"{ungradedCount} ungraded", 0);
        if (deadline == null) return ("ok", "No deadline", 5);

        var now = DateTime.Now;
        var due = EndOfDayIfDateOnly(deadline.Value);
        if (due >= now && due <= now.AddDays(3)) return ("warning", "Closing soon", 1);
        if (due < now && due >= now.AddDays(-7)) return ("ok", "Recently closed", 2);
        if (due >= now) return ("ok", "Upcoming", 4);
        return ("ok", "Closed", 5);
    }

    private static DateTime EndOfDayIfDateOnly(DateTime value)
    {
        return value.TimeOfDay == TimeSpan.Zero
            ? value.Date.AddDays(1).AddTicks(-1)
            : value;
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

    public class DashboardCourseItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsClosed { get; set; }
        public string? TeacherName { get; set; }
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
