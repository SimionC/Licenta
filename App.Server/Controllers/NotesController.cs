using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        // ============================
        // Helper methods for permissions
        // ============================
        private string GetUserId()
        {
            // Adjust this to your claim setup if needed
            return User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == "sub" || c.Type == "NameIdentifier")?.Value;
        }

        private async Task<bool> UserCanReadNote(NoteModel note, string userId)
        {
            if (note.OwnerId == userId) return true;
            if (note.IsPublic) return true;

            var permission = await _context.NotePermissions
                .FirstOrDefaultAsync(p => p.NoteId == note.Id && p.UserId == userId);
            if (permission?.CanRead == true) return true;

            if (note.CollaborationId.HasValue)
            {
                var isCollabMember = await _context.CollaborationMembers
                    .AnyAsync(m => m.CollaborationId == note.CollaborationId.Value && m.UserId == userId);
                if (isCollabMember) return true;
            }

            return false;
        }

        private async Task<bool> UserCanEditNote(NoteModel note, string userId)
        {
            if (note.OwnerId == userId) return true;

            var permission = await _context.NotePermissions
                .FirstOrDefaultAsync(p => p.NoteId == note.Id && p.UserId == userId);
            if (permission?.CanEdit == true) return true;

            if (note.CollaborationId.HasValue)
            {
                var member = await _context.CollaborationMembers
                    .FirstOrDefaultAsync(m => m.CollaborationId == note.CollaborationId.Value && m.UserId == userId);
                if (member != null && (member.Role == "owner" || member.Role == "editor")) return true;
            }

            return false;
        }

        // ============================
        // Endpoints
        // ============================

        // GET: api/Notes?collaborationId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoteModel>>> GetNotes([FromQuery] int? collaborationId)
        {
            var userId = GetUserId();
            IQueryable<NoteModel> notes = _context.Notes;

            if (collaborationId.HasValue)
            {
                notes = notes.Where(n => n.CollaborationId == collaborationId.Value);
            }

            // Only return notes user can read
            var notesList = await notes.ToListAsync();
            notesList = notesList.Where(note => UserCanReadNote(note, userId).Result).ToList();

            return notesList;
        }

        // GET: api/Notes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NoteModel>> GetNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);

            if (note == null)
                return NotFound();

            var userId = GetUserId();
            if (!await UserCanReadNote(note, userId))
                return Forbid();

            return note;
        }

        // POST: api/Notes
        [HttpPost]
        public async Task<ActionResult<NoteModel>> CreateNote(NoteModel note)
        {
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        // PUT: api/Notes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(int id, NoteModel note)
        {
            if (id != note.Id)
                return BadRequest();

            var existingNote = await _context.Notes.FindAsync(id);
            if (existingNote == null)
                return NotFound();

            var userId = GetUserId();
            if (!await UserCanEditNote(existingNote, userId))
                return Forbid();

            // Update fields
            existingNote.Title = note.Title;
            existingNote.Content = note.Content;
            existingNote.UpdatedAt = DateTime.UtcNow;
            existingNote.IsPublic = note.IsPublic;
            existingNote.IsCode = note.IsCode;
            existingNote.IsLatex = note.IsLatex;
            existingNote.CollaborationId = note.CollaborationId;
            // Add any other fields you need

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
                return NotFound();

            var userId = GetUserId();
            if (!await UserCanEditNote(note, userId))
                return Forbid();

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Notes/{noteId}/permissions
        [HttpPost("{noteId}/permissions")]
        public async Task<IActionResult> AddOrUpdatePermission(int noteId, [FromBody] NotePermissionModel permission)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
                return NotFound("Note not found.");

            // If permission exists, update; else add new
            var existing = await _context.NotePermissions
                .FirstOrDefaultAsync(p => p.NoteId == noteId && p.UserId == permission.UserId);

            if (existing != null)
            {
                existing.CanEdit = permission.CanEdit;
                existing.CanRead = permission.CanRead;
            }
            else
            {
                permission.NoteId = noteId;
                _context.NotePermissions.Add(permission);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Notes/{noteId}/permissions/{userId}
        [HttpDelete("{noteId}/permissions/{userId}")]
        public async Task<IActionResult> RemovePermission(int noteId, string userId)
        {
            var permission = await _context.NotePermissions
                .FirstOrDefaultAsync(p => p.NoteId == noteId && p.UserId == userId);

            if (permission == null)
                return NotFound();

            _context.NotePermissions.Remove(permission);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Notes/{noteId}/permissions
        [HttpGet("{noteId}/permissions")]
        public async Task<ActionResult<IEnumerable<NotePermissionModel>>> GetPermissions(int noteId)
        {
            var permissions = await _context.NotePermissions
                .Where(p => p.NoteId == noteId)
                .ToListAsync();
            return permissions;
        }
    }
}
