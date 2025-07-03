using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Add this attribute to require authentication
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    // Get all notes for the current user
    [HttpGet("my-notes")]
    public async Task<IActionResult> GetMyNotes()
    {
        try
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "Email claim not found" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Get notes from database
            var notes = await _context.Notes
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .Select(n => new
                {
                    id = n.Id,
                    guid = n.Guid,
                    title = !string.IsNullOrWhiteSpace(n.Title) ? n.Title : "Untitled Note",
                    content = n.Text ?? "",
                    createdAt = n.CreationDate,
                    updatedAt = n.ModifyDate,
                    isPublic = n.VisibilityTypeId == 2
                })
                .ToListAsync();

            return Ok(notes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting notes: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // Create a new note
    [HttpPost("create")]
    public async Task<IActionResult> CreateNote([FromBody] NoteModel model)
    {
        try
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "Email claim not found" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Generate unique GUID for the note
            var noteGuid = Guid.NewGuid().ToString();

            // Use the provided title, or extract from content as fallback
            var title = !string.IsNullOrWhiteSpace(model.Title) ? model.Title : ExtractTitle(model.Content);

            var note = new Note
            {
                Guid = noteGuid,
                Title = title,
                Text = model.Content ?? "# Welcome to your new note\n\nStart writing here...",
                CreationDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                UserId = user.Id,
                VisibilityTypeId = model.IsPublic ? 2 : 1,
                CollaborationId = model.CollaborationId
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = note.Id,
                guid = note.Guid,
                title = note.Title,
                content = note.Text,
                createdAt = note.CreationDate,
                updatedAt = note.ModifyDate,
                isPublic = note.VisibilityTypeId == 2,
                isOwner = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating note: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // Get a specific note by GUID
    [HttpGet("{guid}")]
    public async Task<IActionResult> GetNote(string guid)
    {
        try
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound(new { error = "Note not found" });
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "Email claim not found" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Check access permissions
            bool hasAccess = note.UserId == user.Id ||
                            note.VisibilityTypeId == 2 ||
                            (note.CollaborationId.HasValue &&
                             await _context.CollaborationMembers.AnyAsync(cm =>
                                cm.CollaborationId == note.CollaborationId && cm.UserId == user.Id));

            if (!hasAccess)
            {
                return Forbid("Access denied");
            }

            var title = !string.IsNullOrWhiteSpace(note.Title) ? note.Title : ExtractTitle(note.Text);

            return Ok(new
            {
                id = note.Id,
                guid = note.Guid,
                title = title,
                content = note.Text,
                createdAt = note.CreationDate,
                updatedAt = note.ModifyDate,
                isPublic = note.VisibilityTypeId == 2,
                isOwner = note.UserId == user.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting note: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // Update a note
    [HttpPut("{guid}")]
    public async Task<IActionResult> UpdateNote(string guid, [FromBody] NoteModel model)
    {
        try
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound(new { error = "Note not found" });
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "Email claim not found" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Check edit permissions
            bool canEdit = note.UserId == user.Id ||
                          (note.CollaborationId.HasValue &&
                           await _context.CollaborationMembers.AnyAsync(cm =>
                              cm.CollaborationId == note.CollaborationId && cm.UserId == user.Id));

            if (!canEdit)
            {
                return Forbid("You don't have permission to edit this note");
            }

            // Update the note
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                note.Title = model.Title;
            }
            else
            {
                note.Title = ExtractTitle(model.Content);
            }

            note.Text = model.Content ?? note.Text;
            note.ModifyDate = DateTime.UtcNow;

            if (model.IsPublic != (note.VisibilityTypeId == 2))
            {
                note.VisibilityTypeId = model.IsPublic ? 2 : 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = note.Id,
                guid = note.Guid,
                title = note.Title,
                content = note.Text,
                createdAt = note.CreationDate,
                updatedAt = note.ModifyDate,
                isPublic = note.VisibilityTypeId == 2,
                isOwner = note.UserId == user.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating note: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // Delete a note
    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeleteNote(string guid)
    {
        try
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound(new { error = "Note not found" });
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "Email claim not found" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Only owner can delete
            if (note.UserId != user.Id)
            {
                return Forbid("Only the owner can delete this note");
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting note: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // Debug endpoint to check authentication
    [HttpGet("debug-info")]
    public IActionResult DebugInfo()
    {
        try
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value;

            return Ok(new
            {
                email = email,
                isAuthenticated = User.Identity.IsAuthenticated,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                authType = User.Identity.AuthenticationType,
                name = User.Identity.Name
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
        }
    }

    // Helper method to extract title from markdown content
    private string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "Untitled Note";

        var lines = content.Split('\n');
        var firstLine = lines.FirstOrDefault()?.Trim();

        if (firstLine?.StartsWith("# ") == true)
        {
            return firstLine.Substring(2).Trim();
        }

        // If no markdown title, use first non-empty line up to 50 chars
        var firstNonEmptyLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (firstNonEmptyLine != null)
        {
            return firstNonEmptyLine.Length > 50 ? firstNonEmptyLine.Substring(0, 50) + "..." : firstNonEmptyLine;
        }

        return "Untitled Note";
    }
}