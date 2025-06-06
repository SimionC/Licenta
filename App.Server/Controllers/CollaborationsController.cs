using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollaborationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CollaborationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Collaborations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollaborationModel>>> GetCollaborations()
        {
            return await _context.Collaborations
                .Include(c => c.Members)
                .Include(c => c.Notes)
                .ToListAsync();
        }

        // GET: api/Collaborations
        [HttpGet("{id}")]
        public async Task<ActionResult<CollaborationModel>> GetCollaboration(int id)
        {
            var collab = await _context.Collaborations
                .Include(c => c.Members)
                .Include(c => c.Notes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collab == null)
                return NotFound();

            return collab;
        }

        // POST: api/Collaborations
        [HttpPost]
        public async Task<ActionResult<CollaborationModel>> CreateCollaboration(CollaborationModel collaboration)
        {
            collaboration.CreatedAt = DateTime.UtcNow;
            _context.Collaborations.Add(collaboration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaboration);
        }

        // PUT: api/Collaborations
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollaboration(int id, CollaborationModel collaboration)
        {
            if (id != collaboration.Id)
                return BadRequest();

            var existingCollab = await _context.Collaborations.FindAsync(id);
            if (existingCollab == null)
                return NotFound();

            existingCollab.Name = collaboration.Name;
            // Optionally: update more fields
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Collaborations
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollaboration(int id)
        {
            var collab = await _context.Collaborations.FindAsync(id);
            if (collab == null)
                return NotFound();

            _context.Collaborations.Remove(collab);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
