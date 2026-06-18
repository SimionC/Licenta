using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Server.ORM;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

//Purpose: Full notes CRUD + access control (owner/public/collaboration member).
//Inputs/Outputs: Uses current user claims + note guid/collaboration id; maps Note entity to NoteModel DTO.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/Models/NoteModel.cs (namespace currently App.Server.ORM).

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


        //Trigger: GET api/Notes/test-auth.
        //Guards: Controller-level authorize.
        //Actions: Dumps auth state and claims.
        //Result: Debug auth snapshot.
        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            var userId = GetCurrentUserId();
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                UserId = userId,
                UserName = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }


        //Trigger: GET api/Notes/my-notes.
        //Guards: Requires current user id.
        //Actions: Filters notes by owner, orders recent first, maps DTO.
        //Result: User-owned notes list.
        [HttpGet("my-notes")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetMyNotes([FromQuery] int? folderId, [FromQuery] bool noFolder = false)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            if (folderId != null && !await UserOwnsFolder(userId.Value, folderId.Value))
                return Forbid();

            var query = _context.Notes
                .Include(n => n.Folder)
                .Where(n => n.UserId == userId.Value);

            if (folderId != null)
                query = query.Where(n => n.FolderId == folderId.Value);

            if (noFolder)
                query = query.Where(n => n.FolderId == null);

            var notes = await query
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .ToListAsync();

            return Ok(notes.Select(MapNote));
        }


        //Trigger: GET api/Notes/accessible-notes.
        //Guards: Requires current user id.
        //Actions: Combines own notes + collaboration notes + public notes.
        //Result: Aggregated accessible list.
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


        //Trigger: GET note by api/Notes/guid(Globally Unique Identifier.
        //Guards: Denies unless owner, public, or collaboration member.
        //Actions: Loads note with related entities, computes role.
        //Result: Note DTO + X-User-Role response header.
        [HttpGet("{guid}")]
        public async Task<ActionResult<NoteModel>> GetNote(string guid)
        {
            var note = await _context.Notes
                .Include(n => n.User)
                .Include(n => n.VisibilityType)
                .Include(n => n.Collaboration)
                .Include(n => n.Folder)
                .FirstOrDefaultAsync(n => n.Guid == guid);

            if (note == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();

            // Check if user can access this note
            bool canAccess = false;
            string? userRole = null;

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

            Response.Headers["X-User-Role"] = userRole ?? string.Empty;

            return Ok(MapNote(note));
        }


        //Trigger: POST api/Notes/create.
        //Guards: Requires auth user; if collaborationId is set, user must be a member.
        //Actions: Creates note with new guid and visibility flag.
        //Result: 201 Created + note DTO.
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
            else if (noteModel.FolderId != null && !await UserOwnsFolder(userId.Value, noteModel.FolderId.Value))
            {
                return Forbid("You cannot use a folder owned by another user");
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
                CollaborationId = noteModel.CollaborationId,
                FolderId = noteModel.CollaborationId == null ? noteModel.FolderId : null
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNote), new { guid = note.Guid }, MapNote(note));
        }


        //Trigger: PUT by  api/Notes/guid.
        //Guards: Owner or collaboration owner/editor only.
        //Actions: Updates title/content/visibility/timestamp; owner may change collaboration link.
        //Result: Updated note DTO.
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
                if (note.CollaborationId == null)
                {
                    if (noteModel.FolderId != null && !await UserOwnsFolder(userId.Value, noteModel.FolderId.Value))
                        return Forbid("You cannot use a folder owned by another user");

                    note.FolderId = noteModel.FolderId;
                }
                else
                {
                    note.FolderId = null;
                }
            }

            _context.Entry(note).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            if (note.FolderId != null)
                await _context.Entry(note).Reference(n => n.Folder).LoadAsync();

            return Ok(MapNote(note));
        }

        //Trigger: DELETE by  api/Notes/guid.
        //Guards: Only owner can delete.
        //Actions: Removes note.
        //Result: 204 NoContent.
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


        //Trigger: GET  api/Notes/public.
        //Guards: Auth required by controller.
        //Actions: Returns only public notes with truncated preview content.
        //Result: Public notes feed.
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


        //Trigger: GET api/Notes/collaboration/{id}.
        //Guards: User must be member of collaboration.
        //Actions: Reads all notes linked to collaboration.
        //Result: Collaboration note list.
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
                .Include(n => n.Folder)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .ToListAsync();

            return Ok(notes.Select(MapNote));
        }


        //Trigger: GET api/Notes/my-collaborations-with-notes.
        //Guards: Requires user id.
        //Actions: Reads memberships + counts notes per collaboration.
        //Result: Collaboration summaries for sidebar/list views.
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

        //Trigger: Internal helper for many endpoints.
        //Guards: Returns null when unauthenticated or claim missing.
        //Actions: Tries multiple claim names.
        //Result: Nullable int user id.
        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = User.FindFirst("userId") ??
                             User.FindFirst(ClaimTypes.NameIdentifier) ??
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

        private async Task<bool> UserOwnsFolder(int userId, int folderId)
        {
            return await _context.NoteFolders.AnyAsync(f => f.Id == folderId && f.UserId == userId);
        }

        private static NoteModel MapNote(Note note)
        {
            return new NoteModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Text,
                CreatedAt = note.CreationDate,
                UpdatedAt = note.ModifyDate,
                UserId = note.UserId,
                IsPublic = note.VisibilityTypeId == 2,
                CollaborationId = note.CollaborationId,
                FolderId = note.FolderId,
                FolderName = note.Folder?.Name,
                Guid = note.Guid
            };
        }
    }
}
