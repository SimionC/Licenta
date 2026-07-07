using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Server.ORM;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text;

//Purpose: Full notes CRUD + access control (owner/direct share/collaboration member).

namespace App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        // -------------------------
        // AUTH DEBUG ENDPOINT
        // -------------------------
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


        // -------------------------
        // NOTE LISTING ENDPOINTS
        // -------------------------
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
                .Where(n => n.UserId == userId.Value && n.CollaborationId == null);

            if (folderId != null)
                query = query.Where(n => n.FolderId == folderId.Value);

            if (noFolder)
                query = query.Where(n => n.FolderId == null);

            var notes = await query
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .ToListAsync();

            return Ok(notes.Select(note => MapNote(note)));
        }

        [HttpGet("shared-with-me")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetSharedWithMe()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var permissions = await _context.NotePermissions
                .Include(p => p.Note)
                    .ThenInclude(n => n.User)
                .Include(p => p.Note)
                    .ThenInclude(n => n.Folder)
                .Where(p =>
                    p.UserId == userId.Value &&
                    p.Status == "accepted" &&
                    p.Note.CollaborationId == null &&
                    p.Note.UserId != userId.Value)
                .OrderByDescending(p => p.Note.ModifyDate ?? p.Note.CreationDate)
                .ToListAsync();

            return Ok(permissions.Select(p => MapNote(
                p.Note,
                p.Role,
                p.Role == "editor",
                false
            )));
        }

        [HttpGet("accessible-notes")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetAccessibleNotes()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var userCollaborationIds = await _context.CollaborationMembers
                .Where(cm => cm.UserId == userId.Value)
                .Select(cm => cm.CollaborationId)
                .ToListAsync();

            var directSharedNoteIds = await _context.NotePermissions
                .Where(p => p.UserId == userId.Value && p.Status == "accepted")
                .Select(p => p.NoteId)
                .ToListAsync();

            var notes = await _context.Notes
                .Include(n => n.User)
                .Include(n => n.Folder)
                .Where(n =>
                    (n.UserId == userId.Value && n.CollaborationId == null) ||
                    (n.CollaborationId != null && userCollaborationIds.Contains(n.CollaborationId.Value)) ||
                    (n.CollaborationId == null && directSharedNoteIds.Contains(n.Id))
                )
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .ToListAsync();

            var collaborationRoles = await _context.CollaborationMembers
                .Where(cm => cm.UserId == userId.Value)
                .ToDictionaryAsync(cm => cm.CollaborationId, cm => cm.Role);

            var directSharedRoles = await _context.NotePermissions
                .Where(p => p.UserId == userId.Value && p.Status == "accepted")
                .ToDictionaryAsync(p => p.NoteId, p => p.Role);

            return Ok(notes.Select(note =>
            {
                if (note.CollaborationId != null && collaborationRoles.TryGetValue(note.CollaborationId.Value, out var collaborationRole))
                {
                    return MapNote(note, collaborationRole, IsCollaborationEditorRole(collaborationRole), false);
                }

                if (note.UserId == userId.Value)
                {
                    return MapNote(note);
                }

                var role = directSharedRoles.TryGetValue(note.Id, out var directRole) ? directRole : "viewer";
                return MapNote(note, role, role == "editor", false);
            }));
        }

        // -------------------------
        // SINGLE NOTE ACCESS
        // -------------------------
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
            bool canEdit = false;
            bool canManageSharing = false;

            // Collaboration notes are governed by collaboration membership, not personal ownership.
            if (note.CollaborationId != null && currentUserId != null)
            {
                var collaborationMember = await _context.CollaborationMembers
                    .FirstOrDefaultAsync(cm => cm.CollaborationId == note.CollaborationId && cm.UserId == currentUserId.Value);

                if (collaborationMember != null)
                {
                    canAccess = true;
                    userRole = collaborationMember.Role;
                    canEdit = IsCollaborationEditorRole(collaborationMember.Role);
                    canManageSharing = false;
                }
            }
            // Owner can always access personal notes
            else if (note.UserId == currentUserId)
            {
                canAccess = true;
                userRole = "owner";
                canEdit = true;
                canManageSharing = true;
            }
            // Direct note sharing for personal notes
            else if (note.CollaborationId == null && currentUserId != null)
            {
                var permission = await _context.NotePermissions
                    .FirstOrDefaultAsync(p =>
                        p.NoteId == note.Id &&
                        p.UserId == currentUserId.Value &&
                        p.Status == "accepted");

                if (permission != null)
                {
                    canAccess = true;
                    userRole = permission.Role;
                    canEdit = permission.Role == "editor";
                }
            }
            if (!canAccess)
            {
                return Forbid();
            }

            Response.Headers["X-User-Role"] = userRole ?? string.Empty;
            Response.Headers["X-Can-Edit"] = canEdit.ToString();
            Response.Headers["X-Can-Manage-Sharing"] = canManageSharing.ToString();

            return Ok(MapNote(note, userRole ?? "viewer", canEdit, canManageSharing));
        }

        // -------------------------
        // NOTE EXPORT
        // -------------------------
        [HttpGet("{guid}/download")]
        public async Task<IActionResult> DownloadNote(string guid, [FromQuery] string? format = "md")
        {
            var requestedFormat = string.IsNullOrWhiteSpace(format)
                ? "md"
                : format.Trim().ToLowerInvariant();

            if (requestedFormat != "md")
            {
                return BadRequest(new { message = "Only markdown downloads are supported." });
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var canAccess = note.UserId == currentUserId.Value;

            if (!canAccess && note.CollaborationId != null)
            {
                canAccess = await _context.CollaborationMembers.AnyAsync(cm =>
                    cm.CollaborationId == note.CollaborationId.Value &&
                    cm.UserId == currentUserId.Value);
            }

            if (!canAccess && note.CollaborationId == null)
            {
                canAccess = await _context.NotePermissions.AnyAsync(p =>
                    p.NoteId == note.Id &&
                    p.UserId == currentUserId.Value &&
                    p.Status == "accepted");
            }

            if (!canAccess)
            {
                return Forbid();
            }

            var title = string.IsNullOrWhiteSpace(note.Title) ? "Untitled Note" : note.Title.Trim();
            var markdown = $"# {title}\n\n{note.Text ?? string.Empty}";
            var bytes = Encoding.UTF8.GetBytes(markdown);
            var fileName = $"{SanitizeFileName(title)}.md";

            return File(bytes, "text/markdown; charset=utf-8", fileName);
        }

 
        // -------------------------
        // DIRECT NOTE SHARING
        // -------------------------
        [HttpGet("{guid}/permissions")]
        public async Task<ActionResult<IEnumerable<NotePermissionModel>>> GetPermissions(string guid)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var note = await GetOwnedPersonalNote(guid, userId.Value);
            if (note == null) return NotFoundOrForbiddenPersonalNote(guid, userId.Value);

            var permissions = await _context.NotePermissions
                .Include(p => p.User)
                .Where(p => p.NoteId == note.Id)
                .OrderBy(p => p.User.Email)
                .ToListAsync();

            return Ok(permissions.Select(MapPermission));
        }

        [HttpPost("{guid}/permissions")]
        public async Task<ActionResult<NotePermissionModel>> AddPermission(string guid, NotePermissionSaveModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var note = await GetOwnedPersonalNote(guid, userId.Value);
            if (note == null) return NotFoundOrForbiddenPersonalNote(guid, userId.Value);

            var role = NormalizePermissionRole(model.Role);
            if (role == null) return BadRequest(new { message = "Role must be viewer or editor." });

            var email = model.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { message = "Email is required." });

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (targetUser == null) return NotFound(new { message = "No registered user was found with that email." });
            if (targetUser.Id == userId.Value) return BadRequest(new { message = "You cannot share a note with yourself." });

            var duplicate = await _context.NotePermissions
                .AnyAsync(p => p.NoteId == note.Id && p.UserId == targetUser.Id);
            if (duplicate) return Conflict(new { message = "This user already has access to this note." });

            var now = DateTime.UtcNow;
            var permission = new NotePermission
            {
                NoteId = note.Id,
                UserId = targetUser.Id,
                Role = role,
                Status = "accepted",
                InvitedByUserId = userId.Value,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.NotePermissions.Add(permission);
            await _context.SaveChangesAsync();

            permission.User = targetUser;
            return CreatedAtAction(nameof(GetPermissions), new { guid }, MapPermission(permission));
        }

        [HttpPut("{guid}/permissions/{permissionId}")]
        public async Task<ActionResult<NotePermissionModel>> UpdatePermission(string guid, int permissionId, NotePermissionSaveModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var note = await GetOwnedPersonalNote(guid, userId.Value);
            if (note == null) return NotFoundOrForbiddenPersonalNote(guid, userId.Value);

            var role = NormalizePermissionRole(model.Role);
            if (role == null) return BadRequest(new { message = "Role must be viewer or editor." });

            var permission = await _context.NotePermissions
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == permissionId && p.NoteId == note.Id);

            if (permission == null) return NotFound(new { message = "Permission not found." });

            permission.Role = role;
            permission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapPermission(permission));
        }

        [HttpDelete("{guid}/permissions/{permissionId}")]
        public async Task<IActionResult> DeletePermission(string guid, int permissionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var note = await GetOwnedPersonalNote(guid, userId.Value);
            if (note == null) return NotFoundOrForbiddenPersonalNote(guid, userId.Value);

            var permission = await _context.NotePermissions
                .FirstOrDefaultAsync(p => p.Id == permissionId && p.NoteId == note.Id);

            if (permission == null) return NotFound(new { message = "Permission not found." });

            _context.NotePermissions.Remove(permission);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // -------------------------
        // NOTE CREATE / UPDATE / DELETE
        // -------------------------
        [HttpPost("create")]
        public async Task<ActionResult<NoteModel>> CreateNote(NoteModel noteModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // If collaboration is specified, check if user can create inside it
            if (noteModel.CollaborationId != null)
            {
                var memberRole = await _context.CollaborationMembers
                    .Where(cm => cm.CollaborationId == noteModel.CollaborationId && cm.UserId == userId.Value)
                    .Select(cm => cm.Role)
                    .FirstOrDefaultAsync();

                if (memberRole == null)
                {
                    return Forbid("You are not a member of this collaboration");
                }

                if (!IsCollaborationEditorRole(memberRole))
                {
                    return Forbid("You do not have permission to create notes in this collaboration");
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
                VisibilityTypeId = 1,
                CollaborationId = noteModel.CollaborationId,
                FolderId = noteModel.CollaborationId == null ? noteModel.FolderId : null
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNote), new { guid = note.Guid }, MapNote(note));
        }


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

            bool canEdit = false;
            bool isOwner = note.UserId == userId.Value && note.CollaborationId == null;
            bool isDirectSharedEditor = false;
            string? collaborationRole = null;

            if (note.CollaborationId != null)
            {
                collaborationRole = await _context.CollaborationMembers
                    .Where(cm => cm.CollaborationId == note.CollaborationId && cm.UserId == userId.Value)
                    .Select(cm => cm.Role)
                    .FirstOrDefaultAsync();

                canEdit = IsCollaborationEditorRole(collaborationRole);
            }
            else if (isOwner)
            {
                canEdit = true;
            }
            // Direct shared editors can only edit personal note title/content
            else
            {
                isDirectSharedEditor = await _context.NotePermissions
                    .AnyAsync(p =>
                        p.NoteId == note.Id &&
                        p.UserId == userId.Value &&
                        p.Status == "accepted" &&
                        p.Role == "editor");

                canEdit = isDirectSharedEditor;
            }

            if (!canEdit)
            {
                return Forbid("You don't have permission to edit this note");
            }

            // Update note properties
            note.Title = noteModel.Title ?? note.Title;
            note.Text = noteModel.Content ?? note.Text;
            note.ModifyDate = DateTime.UtcNow;

            if (note.CollaborationId != null)
            {
                note.VisibilityTypeId = 1;
                note.FolderId = null;
            }
            // Only the owner can change visibility and personal folder placement for personal notes.
            else if (isOwner)
            {
                note.VisibilityTypeId = 1;
                note.CollaborationId = null;
                if (noteModel.FolderId != null && !await UserOwnsFolder(userId.Value, noteModel.FolderId.Value))
                    return Forbid("You cannot use a folder owned by another user");

                note.FolderId = noteModel.FolderId;
            }

            _context.Entry(note).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            if (note.FolderId != null)
                await _context.Entry(note).Reference(n => n.Folder).LoadAsync();

            return Ok(MapNote(
                note,
                note.CollaborationId != null ? collaborationRole ?? "editor" : isOwner ? "owner" : isDirectSharedEditor ? "editor" : "editor",
                true,
                isOwner
            ));
        }


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

            if (note.CollaborationId != null)
            {
                var memberRole = await _context.CollaborationMembers
                    .Where(cm => cm.CollaborationId == note.CollaborationId && cm.UserId == userId.Value)
                    .Select(cm => cm.Role)
                    .FirstOrDefaultAsync();

                if (!IsCollaborationEditorRole(memberRole))
                {
                    return Forbid("You do not have permission to delete notes in this collaboration");
                }
            }
            // Only owner can delete personal notes
            else if (note.UserId != userId.Value)
            {
                return Forbid("Only the note owner can delete this note");
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // -------------------------
        // COLLABORATION SUMMARY FOR NOTES PAGE
        // -------------------------
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

        // -------------------------
        // AUTH, OWNERSHIP, AND ROLE HELPERS
        // -------------------------
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

        private async Task<Note?> GetOwnedPersonalNote(string guid, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Guid == guid);
            if (note == null || note.UserId != userId || note.CollaborationId != null)
            {
                return null;
            }

            return note;
        }

        private ActionResult NotFoundOrForbiddenPersonalNote(string guid, int userId)
        {
            var noteExists = _context.Notes.Any(n => n.Guid == guid);
            if (!noteExists) return NotFound(new { message = "Note not found." });

            var isCollaborationNote = _context.Notes.Any(n => n.Guid == guid && n.CollaborationId != null && n.UserId == userId);
            if (isCollaborationNote) return BadRequest(new { message = "Direct sharing is only available for personal notes." });

            return Forbid();
        }

        private static string? NormalizePermissionRole(string? role)
        {
            var normalized = role?.Trim().ToLowerInvariant();
            return normalized == "viewer" || normalized == "editor" ? normalized : null;
        }

        private static bool IsCollaborationEditorRole(string? role)
        {
            return role == "owner" || role == "editor";
        }

        // -------------------------
        // FORMATTING AND DTO MAPPING
        // -------------------------
        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", value.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "note" : sanitized;
        }

        private static NoteModel MapNote(
            Note note,
            string accessRole = "owner",
            bool canEdit = true,
            bool canManageSharing = true)
        {
            return new NoteModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Text,
                CreatedAt = note.CreationDate,
                UpdatedAt = note.ModifyDate,
                UserId = note.UserId,
                IsPublic = false,
                CollaborationId = note.CollaborationId,
                FolderId = note.FolderId,
                FolderName = note.Folder?.Name,
                AccessRole = accessRole,
                CanEdit = canEdit,
                CanManageSharing = canManageSharing,
                OwnerEmail = note.User?.Email,
                Guid = note.Guid
            };
        }

        private static NotePermissionModel MapPermission(NotePermission permission)
        {
            return new NotePermissionModel
            {
                Id = permission.Id,
                UserId = permission.UserId,
                Email = permission.User.Email,
                DisplayName = $"{permission.User.Prenume} {permission.User.Nume}".Trim(),
                Role = permission.Role,
                Status = permission.Status,
                CreatedAt = permission.CreatedAt,
                UpdatedAt = permission.UpdatedAt
            };
        }
    }

    // -------------------------
    // REQUEST / RESPONSE MODELS
    // -------------------------
    public class NotePermissionSaveModel
    {
        public string? Email { get; set; }
        public string? Role { get; set; }
    }

    public class NotePermissionModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "viewer";
        public string Status { get; set; } = "accepted";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
