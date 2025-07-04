using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Server.ORM;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Notes/test-auth
        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            var userId = GetCurrentUserId();
            return Ok(new
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserId = userId,
                UserName = User.Identity.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        // GET: api/Notes/my-notes
        [HttpGet("my-notes")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetMyNotes()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var notes = await _context.Notes
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .Select(n => new NoteModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Text, // Map Text to Content
                    CreatedAt = n.CreationDate,
                    UpdatedAt = n.ModifyDate,
                    UserId = n.UserId,
                    IsPublic = n.VisibilityTypeId == 2, // 2 = public, 1 = private
                    CollaborationId = n.CollaborationId,
                    Guid = n.Guid
                })
                .ToListAsync();

            return Ok(notes);
        }

        // GET: api/Notes/{guid}
        [HttpGet("{guid}")]
        public async Task<ActionResult<NoteModel>> GetNote(string guid)
        {
            var note = await _context.Notes
                .Include(n => n.User)
                .Include(n => n.VisibilityType)
                .FirstOrDefaultAsync(n => n.Guid == guid);

            if (note == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();

            // Check if user can access this note
            if (note.UserId != currentUserId && note.VisibilityTypeId != 2) // Not owner and not public
            {
                return Forbid();
            }

            var noteModel = new NoteModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Text,
                CreatedAt = note.CreationDate,
                UpdatedAt = note.ModifyDate,
                UserId = note.UserId,
                IsPublic = note.VisibilityTypeId == 2,
                CollaborationId = note.CollaborationId,
                Guid = note.Guid
            };

            return Ok(noteModel);
        }

        // POST: api/Notes/create
        [HttpPost("create")]
        public async Task<ActionResult<NoteModel>> CreateNote(NoteModel noteModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var note = new Note
            {
                Title = noteModel.Title ?? "Untitled Note",
                Guid = System.Guid.NewGuid().ToString(),
                Text = noteModel.Content ?? "",
                CreationDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                UserId = userId.Value,
                VisibilityTypeId = noteModel.IsPublic ? 2 : 1, // 2 = public, 1 = private
                CollaborationId = noteModel.CollaborationId
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            var createdNoteModel = new NoteModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Text,
                CreatedAt = note.CreationDate,
                UpdatedAt = note.ModifyDate,
                UserId = note.UserId,
                IsPublic = note.VisibilityTypeId == 2,
                CollaborationId = note.CollaborationId,
                Guid = note.Guid
            };

            return CreatedAtAction(nameof(GetNote), new { guid = note.Guid }, createdNoteModel);
        }

        // PUT: api/Notes/{guid}
        [HttpPut("{guid}")]
        public async Task<ActionResult<NoteModel>> UpdateNote(string guid, NoteModel noteModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound();
            }

            // Check if user owns this note
            if (note.UserId != userId.Value)
            {
                return Forbid();
            }

            // Update note properties
            note.Title = noteModel.Title ?? note.Title;
            note.Text = noteModel.Content ?? note.Text;
            note.ModifyDate = DateTime.UtcNow;
            note.VisibilityTypeId = noteModel.IsPublic ? 2 : 1;
            note.CollaborationId = noteModel.CollaborationId;

            _context.Entry(note).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var updatedNoteModel = new NoteModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Text,
                CreatedAt = note.CreationDate,
                UpdatedAt = note.ModifyDate,
                UserId = note.UserId,
                IsPublic = note.VisibilityTypeId == 2,
                CollaborationId = note.CollaborationId,
                Guid = note.Guid
            };

            return Ok(updatedNoteModel);
        }

        // DELETE: api/Notes/{guid}
        [HttpDelete("{guid}")]
        public async Task<IActionResult> DeleteNote(string guid)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound();
            }

            // Check if user owns this note
            if (note.UserId != userId.Value)
            {
                return Forbid();
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Notes/public
        [HttpGet("public")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetPublicNotes()
        {
            var notes = await _context.Notes
                .Where(n => n.VisibilityTypeId == 2) // Public notes
                .Include(n => n.User)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .Select(n => new NoteModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Text.Length > 200 ? n.Text.Substring(0, 200) + "..." : n.Text,
                    CreatedAt = n.CreationDate,
                    UpdatedAt = n.ModifyDate,
                    UserId = n.UserId,
                    IsPublic = true,
                    CollaborationId = n.CollaborationId,
                    Guid = n.Guid
                })
                .ToListAsync();

            return Ok(notes);
        }

        // GET: api/Notes/shared/{collaborationId}
        [HttpGet("shared/{collaborationId}")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetSharedNotes(int collaborationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // TODO: Add logic to check if user is part of the collaboration
            // For now, just return notes with the collaboration ID

            var notes = await _context.Notes
                .Where(n => n.CollaborationId == collaborationId)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .Select(n => new NoteModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Text,
                    CreatedAt = n.CreationDate,
                    UpdatedAt = n.ModifyDate,
                    UserId = n.UserId,
                    IsPublic = n.VisibilityTypeId == 2,
                    CollaborationId = n.CollaborationId,
                    Guid = n.Guid
                })
                .ToListAsync();

            return Ok(notes);
        }

        private int? GetCurrentUserId()
        {
            // Debug: Log all claims to see what's available
            Console.WriteLine("=== User Claims ===");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }
            Console.WriteLine("=== End Claims ===");

            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("User is not authenticated");
                return null;
            }

            // Try different claim types commonly used for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                             User.FindFirst("sub") ??
                             User.FindFirst("id") ??
                             User.FindFirst("UserId") ??
                             User.FindFirst("user_id");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                Console.WriteLine($"Found user ID: {userId}");
                return userId;
            }

            Console.WriteLine("No valid user ID claim found");
            return null;
        }
    }
}