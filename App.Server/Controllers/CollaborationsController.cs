using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Server.ORM;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

//Purpose: Collaboration CRUD, membership roles, invites, and member management.
//Inputs/Outputs: Uses current user identity and body models; returns collaboration summary/detail shapes.
//Depends on: App.Server/ORM/AppDbContext.cs, inline model classes at file bottom.

namespace App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CollaborationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CollaborationsController(AppDbContext context)
        {
            _context = context;
        }


        //Trigger: GET api/Collaborations/my-collaborations.
        //Guards: Requires current user id.
        //Actions: Joins membership + collaboration + creator + member count.
        //Result: List of collaborations user belongs to.
        [HttpGet("my-collaborations")]
        public async Task<ActionResult<IEnumerable<CollaborationModel>>> GetMyCollaborations()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var collaborations = await _context.CollaborationMembers
                .Where(cm => cm.UserId == userId.Value)
                .Include(cm => cm.Collaboration)
                .Include(cm => cm.Collaboration.User)
                .Select(cm => new CollaborationModel
                {
                    Id = cm.Collaboration.Id,
                    Name = cm.Collaboration.Name,
                    CreatedAt = cm.Collaboration.CreatedAt,
                    CreatedBy = cm.Collaboration.User.Email,
                    MyRole = cm.Role,
                    MemberCount = _context.CollaborationMembers
                        .Count(m => m.CollaborationId == cm.Collaboration.Id)
                })
                .ToListAsync();

            return Ok(collaborations);
        }

        
        //Trigger: GET collaboration by api/Collaborations/{id}.
        //Guards: User must be member.
        //Actions: Loads collaboration and member details with roles.
        //Result: Collaboration detail payload.
        [HttpGet("{id}")]
        public async Task<ActionResult<CollaborationDetailModel>> GetCollaboration(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is member of this collaboration
            var currentMember = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.CollaborationId == id && cm.UserId == userId.Value);

            if (currentMember == null)
            {
                return Forbid();
            }

            var collaboration = await _context.Collaborations
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collaboration == null)
            {
                return NotFound();
            }

            var members = await _context.CollaborationMembers
                .Where(cm => cm.CollaborationId == id)
                .Include(cm => cm.User)
                .Select(cm => new CollaborationMemberModel
                {
                    Id = cm.Id,
                    Email = cm.User.Email,
                    Role = cm.Role,
                    IsOwner = cm.UserId == collaboration.UserId
                })
                .ToListAsync();

            var collaborationDetail = new CollaborationDetailModel
            {
                Id = collaboration.Id,
                Name = collaboration.Name,
                CreatedAt = collaboration.CreatedAt,
                CreatedBy = collaboration.User.Email,
                MyRole = currentMember.Role,
                Members = members
            };

            return Ok(collaborationDetail);
        }

        //Trigger: GET api/Collaborations/{id}/notes.
        //Guards: Current user must be a collaboration member.
        //Actions: Returns notes contained in the collaboration with role-aware access flags.
        [HttpGet("{id}/notes")]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetCollaborationNotes(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var memberRole = await _context.CollaborationMembers
                .Where(cm => cm.CollaborationId == id && cm.UserId == userId.Value)
                .Select(cm => cm.Role)
                .FirstOrDefaultAsync();

            if (memberRole == null)
            {
                return Forbid();
            }

            var notes = await _context.Notes
                .Where(n => n.CollaborationId == id)
                .OrderByDescending(n => n.ModifyDate ?? n.CreationDate)
                .ToListAsync();

            return Ok(notes.Select(note => MapCollaborationNote(note, memberRole)));
        }

        //Trigger: POST api/Collaborations/{id}/notes.
        //Guards: Current user must be owner/editor in the collaboration.
        //Actions: Creates an internal collaboration note that is not assigned to personal folders.
        [HttpPost("{id}/notes")]
        public async Task<ActionResult<NoteModel>> CreateCollaborationNote(int id, NoteModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var memberRole = await _context.CollaborationMembers
                .Where(cm => cm.CollaborationId == id && cm.UserId == userId.Value)
                .Select(cm => cm.Role)
                .FirstOrDefaultAsync();

            if (memberRole == null)
            {
                return Forbid();
            }

            if (!IsCollaborationEditorRole(memberRole))
            {
                return Forbid("You do not have permission to create notes in this collaboration");
            }

            var collaborationExists = await _context.Collaborations.AnyAsync(c => c.Id == id);
            if (!collaborationExists)
            {
                return NotFound();
            }

            var note = new Note
            {
                Title = string.IsNullOrWhiteSpace(model.Title) ? "Untitled Note" : model.Title,
                Guid = Guid.NewGuid().ToString(),
                Text = model.Content ?? string.Empty,
                CreationDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                UserId = userId.Value,
                VisibilityTypeId = 1,
                CollaborationId = id,
                FolderId = null
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(NotesController.GetNote), "Notes", new { guid = note.Guid }, MapCollaborationNote(note, memberRole));
        }

        
        //Trigger: POST api/Collaborations/create.
        //Guards: Requires auth user id.
        //Actions: Creates collaboration, adds creator as owner, optionally adds invited users.
        //Result: 201 Created with summary model.
        [HttpPost("create")]
        public async Task<ActionResult<CollaborationModel>> CreateCollaboration(CreateCollaborationModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Collaboration name is required");
            }

            var requestedMembers = model.Members?.Any() == true
                ? model.Members
                : (model.MemberEmails ?? new List<string>()).Select(email => new CreateCollaborationMemberModel
                {
                    Email = email,
                    Role = "editor"
                }).ToList();

            var normalizedMembers = new List<CreateCollaborationMemberModel>();
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var requestedMember in requestedMembers)
            {
                var email = requestedMember.Email?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Collaborator email is required");
                }

                var role = NormalizeMemberRole(requestedMember.Role ?? "viewer", allowOwner: false);
                if (role == null)
                {
                    return BadRequest("Collaborator role must be viewer or editor");
                }

                if (seenEmails.Add(email))
                {
                    normalizedMembers.Add(new CreateCollaborationMemberModel
                    {
                        Email = email,
                        Role = role
                    });
                }
            }

            var currentUserEmail = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (currentUserEmail != null && normalizedMembers.Any(m => m.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("You are already the owner of this collaboration");
            }

            var memberEmails = normalizedMembers.Select(m => m.Email).ToList();
            var usersByEmail = await _context.Users
                .Where(u => memberEmails.Contains(u.Email.ToLower()))
                .ToDictionaryAsync(u => u.Email.ToLower());

            var missingEmails = memberEmails.Where(email => !usersByEmail.ContainsKey(email)).ToList();
            if (missingEmails.Any())
            {
                return BadRequest($"No registered user was found for: {string.Join(", ", missingEmails)}");
            }

            // Create the collaboration
            var collaboration = new Collaboration
            {
                Name = name,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Collaborations.Add(collaboration);
            await _context.SaveChangesAsync();

            // Add creator as owner
            var ownerMember = new CollaborationMember
            {
                CollaborationId = collaboration.Id,
                UserId = userId.Value,
                Role = "owner"
            };
            _context.CollaborationMembers.Add(ownerMember);

            // Add invited members
            foreach (var requestedMember in normalizedMembers)
            {
                var user = usersByEmail[requestedMember.Email];
                if (user.Id != userId.Value)
                {
                    var member = new CollaborationMember
                    {
                        CollaborationId = collaboration.Id,
                        UserId = user.Id,
                        Role = requestedMember.Role
                    };
                    _context.CollaborationMembers.Add(member);
                }
            }

            await _context.SaveChangesAsync();

            var collaborationModel = new CollaborationModel
            {
                Id = collaboration.Id,
                Name = collaboration.Name,
                CreatedAt = collaboration.CreatedAt,
                CreatedBy = currentUserEmail ?? User.Identity?.Name ?? string.Empty,
                MyRole = "owner",
                MemberCount = normalizedMembers.Count + 1
            };

            return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaborationModel);
        }

        
        //Trigger: PUT api/Collaborations/{id}/members/{memberId}.
        //Guards: Only collaboration owner can change roles; owner role itself cannot be changed.
        //Actions: Updates target member role.
        //Result: 200 OK.
        [HttpPut("{id}/members/{memberId}")]
        public async Task<IActionResult> UpdateMemberRole(int id, int memberId, UpdateMemberRoleModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is owner of this collaboration
            var collaboration = await _context.Collaborations.FirstOrDefaultAsync(c => c.Id == id);
            if (collaboration == null || collaboration.UserId != userId.Value)
            {
                return Forbid();
            }

            var member = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.Id == memberId && cm.CollaborationId == id);

            if (member == null)
            {
                return NotFound();
            }

            // Don't allow changing owner role
            if (member.UserId == collaboration.UserId)
            {
                return BadRequest("Cannot change owner role");
            }

            var role = NormalizeMemberRole(model.Role, allowOwner: false);
            if (role == null)
            {
                return BadRequest("Role must be viewer or editor");
            }

            member.Role = role;
            await _context.SaveChangesAsync();

            return Ok();
        }

     
        //Trigger: DELETE api/Collaborations/{id}/members/{memberId}.
        //Guards: Only owner can remove; owner cannot remove self-owner entry.
        //Actions: Deletes membership row.
        //Result: 200 OK.
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is owner of this collaboration
            var collaboration = await _context.Collaborations.FirstOrDefaultAsync(c => c.Id == id);
            if (collaboration == null || collaboration.UserId != userId.Value)
            {
                return Forbid();
            }

            var member = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.Id == memberId && cm.CollaborationId == id);

            if (member == null)
            {
                return NotFound();
            }

            // Don't allow removing owner
            if (member.UserId == collaboration.UserId)
            {
                return BadRequest("Cannot remove owner");
            }

            _context.CollaborationMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok();
        }

        
        //Trigger: DELETE collaboration by api/Collaborations/{id}.
        //Guards: Collaboration must exist; requester must be collaboration owner (UserId).
        //Actions: Removes collaboration members, then collaboration.
        //Result: 204 NoContent.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollaboration(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            
            // 1️. find the collaboration
            var collab = await _context.Collaborations
                .FirstOrDefaultAsync(c => c.Id == id);
            if (collab == null)
                return NotFound();
            
            // 2️. verify requester is the collaboration owner
            if (collab.UserId != userId.Value)
                return Forbid();
            
            // 3️. delete only notes and member records that belong to this collaboration
            var notes = _context.Notes
                .Where(n => n.CollaborationId == id);
            _context.Notes.RemoveRange(notes);

            var members = _context.CollaborationMembers
                .Where(cm => cm.CollaborationId == id);
            _context.CollaborationMembers.RemoveRange(members);

            // 4️. delete the collaboration itself
            _context.Collaborations.Remove(collab);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        
        //Trigger: POST api/Collaborations/{id}/invite.
        //Guards: Requester must be owner/editor; invited user must exist and not already be a member.
        //Actions: Adds member with provided/default role.
        //Result: 200 OK.
        [HttpPost("{id}/invite")]
        public async Task<IActionResult> InviteUser(int id, InviteUserModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is owner or editor of this collaboration
            var userMember = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.CollaborationId == id && cm.UserId == userId.Value);

            if (userMember == null || (userMember.Role != "owner" && userMember.Role != "editor"))
            {
                return Forbid();
            }

            var email = model.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return BadRequest("Please enter a valid email address");
            }

            // Check if user exists
            var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (invitedUser == null)
            {
                return BadRequest("No registered user was found with that email");
            }

            // Check if user is already a member
            var existingMember = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.CollaborationId == id && cm.UserId == invitedUser.Id);

            if (existingMember != null)
            {
                return BadRequest("User is already a member");
            }

            // Add new member
            var role = NormalizeMemberRole(model.Role ?? "editor", allowOwner: false);
            if (role == null)
            {
                return BadRequest("Role must be viewer or editor");
            }

            var newMember = new CollaborationMember
            {
                CollaborationId = id,
                UserId = invitedUser.Id,
                Role = role
            };

            _context.CollaborationMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Helper method to get current user ID
        //Trigger: Internal identity parsing.
        //Guards: Null when unauthenticated/missing claims.
        //Actions: Tries several claim keys.
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

        private static string? NormalizeMemberRole(string? role, bool allowOwner)
        {
            var normalized = role?.Trim().ToLowerInvariant();
            if (normalized == "viewer" || normalized == "editor") return normalized;
            if (allowOwner && normalized == "owner") return normalized;
            return null;
        }

        private static bool IsCollaborationEditorRole(string? role)
        {
            return role == "owner" || role == "editor";
        }

        private static NoteModel MapCollaborationNote(Note note, string memberRole)
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
                FolderId = null,
                FolderName = null,
                AccessRole = memberRole,
                CanEdit = IsCollaborationEditorRole(memberRole),
                CanManageSharing = false,
                Guid = note.Guid
            };
        }
    }

    // Models
    public class CollaborationModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string MyRole { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class CollaborationDetailModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string MyRole { get; set; } = string.Empty;
        public List<CollaborationMemberModel> Members { get; set; } = new();
    }

    public class CollaborationMemberModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
    }

    public class CreateCollaborationModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> MemberEmails { get; set; } = new();
        public List<CreateCollaborationMemberModel> Members { get; set; } = new();
    }

    public class CreateCollaborationMemberModel
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "viewer";
    }

    public class UpdateMemberRoleModel
    {
        public string Role { get; set; } = string.Empty;
    }

    public class InviteUserModel
    {
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
