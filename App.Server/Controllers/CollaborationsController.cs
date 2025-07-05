using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Server.ORM;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/Collaborations/my-collaborations
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

        // GET: api/Collaborations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CollaborationDetailModel>> GetCollaboration(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user is member of this collaboration
            var isMember = await _context.CollaborationMembers
                .AnyAsync(cm => cm.CollaborationId == id && cm.UserId == userId.Value);

            if (!isMember)
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
                Members = members
            };

            return Ok(collaborationDetail);
        }

        // POST: api/Collaborations/create
        [HttpPost("create")]
        public async Task<ActionResult<CollaborationModel>> CreateCollaboration(CreateCollaborationModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Create the collaboration
            var collaboration = new Collaboration
            {
                Name = model.Name,
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
            foreach (var email in model.MemberEmails)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null && user.Id != userId.Value) // Don't add creator twice
                {
                    var member = new CollaborationMember
                    {
                        CollaborationId = collaboration.Id,
                        UserId = user.Id,
                        Role = "editor"
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
                CreatedBy = User.Identity.Name,
                MyRole = "owner",
                MemberCount = model.MemberEmails.Count + 1
            };

            return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaborationModel);
        }

        // PUT: api/Collaborations/{id}/members/{memberId}
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

            member.Role = model.Role;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/Collaborations/{id}/members/{memberId}
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

        // DELETE: api/Collaborations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollaboration(int id)
        {
            // 1️⃣ find the collaboration
            var collab = await _context.Collaborations
                .FirstOrDefaultAsync(c => c.Id == id);
            if (collab == null)
                return NotFound();
            // 2️⃣ delete all member records for that collaboration
            var members = _context.CollaborationMembers
                .Where(cm => cm.CollaborationId == id);
            _context.CollaborationMembers.RemoveRange(members);
            // 3️⃣ delete the collaboration itself
            _context.Collaborations.Remove(collab);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Collaborations/{id}/invite
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

            // Check if user exists
            var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (invitedUser == null)
            {
                return BadRequest("User not found");
            }

            // Check if user is already a member
            var existingMember = await _context.CollaborationMembers
                .FirstOrDefaultAsync(cm => cm.CollaborationId == id && cm.UserId == invitedUser.Id);

            if (existingMember != null)
            {
                return BadRequest("User is already a member");
            }

            // Add new member
            var newMember = new CollaborationMember
            {
                CollaborationId = id,
                UserId = invitedUser.Id,
                Role = model.Role ?? "editor"
            };

            _context.CollaborationMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Helper method to get current user ID
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