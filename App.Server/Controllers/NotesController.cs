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

        // GET: api/Notes/accessible-notes
        [HttpGet("accessible-notes")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetAccessibleNotes()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Get user's collaborations
            var userCollaborationIds = await _context.CollaborationMembers
                .Where(cm => cm.UserId == userId.Value)
                .Select(cm => cm.CollaborationId)
                .ToListAsync();

            var notes = await _context.Notes
                .Where(n =>
                    // User's own notes
                    n.UserId == userId.Value ||
                    // Notes in collaborations user is part of
                    (n.CollaborationId != null && userCollaborationIds.Contains(n.CollaborationId.Value)) ||
                    // Public notes
                    n.VisibilityTypeId == 2
                )
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

        // GET: api/Notes/{guid}
        [HttpGet("{guid}")]
        public async Task<ActionResult<NoteModel>> GetNote(string guid)
        {
            var note = await _context.Notes
                .Include(n => n.User)
                .Include(n => n.VisibilityType)
                .Include(n => n.Collaboration)
                .FirstOrDefaultAsync(n => n.Guid == guid);

            if (note == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();

            // Check if user can access this note
            bool canAccess = false;
            string userRole = null;

            // Owner can always access
            if (note.UserId == currentUserId)
            {
                canAccess = true;
                userRole = "owner";
            }
            // Public notes can be accessed by anyone
            else if (note.VisibilityTypeId == 2)
            {
                canAccess = true;
                userRole = "viewer"; // Public access is read-only
            }
            // Check if user is part of the collaboration
            else if (note.CollaborationId != null && currentUserId != null)
            {
                var collaborationMember = await _context.CollaborationMembers
                    .FirstOrDefaultAsync(cm => cm.CollaborationId == note.CollaborationId && cm.UserId == currentUserId.Value);

                if (collaborationMember != null)
                {
                    canAccess = true;
                    userRole = collaborationMember.Role;
                }
            }

            if (!canAccess)
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

            // You might want to add user role information to the response
            Response.Headers.Add("X-User-Role", userRole);

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

            // If collaboration is specified, check if user is a member
            if (noteModel.CollaborationId != null)
            {
                var isMember = await _context.CollaborationMembers
                    .AnyAsync(cm => cm.CollaborationId == noteModel.CollaborationId && cm.UserId == userId.Value);

                if (!isMember)
                {
                    return Forbid("You are not a member of this collaboration");
                }
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

            // Check if user can edit this note
            bool canEdit = false;

            // Owner can always edit
            if (note.UserId == userId.Value)
            {
                canEdit = true;
            }
            // Check if user is part of the collaboration with edit permissions
            else if (note.CollaborationId != null)
            {
                var memberRole = await _context.CollaborationMembers
                    .Where(cm => cm.CollaborationId == note.CollaborationId && cm.UserId == userId.Value)
                    .Select(cm => cm.Role)
                    .FirstOrDefaultAsync();

                canEdit = memberRole == "owner" || memberRole == "editor";
            }

            if (!canEdit)
            {
                return Forbid("You don't have permission to edit this note");
            }

            // Update note properties
            note.Title = noteModel.Title ?? note.Title;
            note.Text = noteModel.Content ?? note.Text;
            note.ModifyDate = DateTime.UtcNow;
            note.VisibilityTypeId = noteModel.IsPublic ? 2 : 1;

            // Only allow owner to change collaboration
            if (note.UserId == userId.Value)
            {
                note.CollaborationId = noteModel.CollaborationId;
            }

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

            // Only owner can delete notes
            if (note.UserId != userId.Value)
            {
                return Forbid("Only the note owner can delete this note");
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

        // GET: api/Notes/collaboration/{collaborationId}
        [HttpGet("collaboration/{collaborationId}")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetCollaborationNotes(int collaborationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is part of the collaboration
            var isMember = await _context.CollaborationMembers
                .AnyAsync(cm => cm.CollaborationId == collaborationId && cm.UserId == userId.Value);

            if (!isMember)
            {
                return Forbid("You are not a member of this collaboration");
            }

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

        // GET: api/Notes/my-collaborations-with-notes
        [HttpGet("my-collaborations-with-notes")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyCollaborationsWithNotes()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var collaborationsWithNotes = await _context.CollaborationMembers
                .Where(cm => cm.UserId == userId.Value)
                .Include(cm => cm.Collaboration)
                .Select(cm => new
                {
                    CollaborationId = cm.Collaboration.Id,
                    CollaborationName = cm.Collaboration.Name,
                    MyRole = cm.Role,
                    NoteCount = _context.Notes.Count(n => n.CollaborationId == cm.Collaboration.Id)
                })
                .ToListAsync();

            return Ok(collaborationsWithNotes);
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return null;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                             User.FindFirst("sub") ??
                             User.FindFirst("id") ??
                             User.FindFirst("UserId") ??
                             User.FindFirst("user_id");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}