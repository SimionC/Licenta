using App.Server.ORM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace App.Server.Controllers;

// Purpose: manages the current user's note folders.
// Main use: Notes page and note editor folder dropdown.

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoteFoldersController : ControllerBase
{
    private readonly AppDbContext _context;

    public NoteFoldersController(AppDbContext context)
    {
        _context = context;
    }

    // -------------------------
    // FOLDERS
    // -------------------------
    [HttpGet("my-folders")]
    public async Task<IActionResult> GetMyFolders()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var folders = await _context.NoteFolders
            .Where(f => f.UserId == userId.Value)
            .OrderBy(f => f.Name)
            .Select(f => new NoteFolderModel
            {
                Id = f.Id,
                Name = f.Name,
                UserId = f.UserId,
                ParentFolderId = f.ParentFolderId,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                NoteCount = _context.Notes.Count(n => n.FolderId == f.Id && n.UserId == userId.Value)
            })
            .ToListAsync();

        return Ok(folders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder(NoteFolderSaveModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var name = model.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Folder name is required.");

        if (model.ParentFolderId != null && !await UserOwnsFolder(userId.Value, model.ParentFolderId.Value))
            return Forbid();

        var folder = new NoteFolder
        {
            Name = name,
            UserId = userId.Value,
            ParentFolderId = model.ParentFolderId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NoteFolders.Add(folder);
        await _context.SaveChangesAsync();

        return Ok(MapFolder(folder, noteCount: 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(int id, NoteFolderSaveModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var folder = await _context.NoteFolders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId.Value);
        if (folder == null) return NotFound();

        var name = model.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Folder name is required.");

        if (model.ParentFolderId == id)
            return BadRequest("A folder cannot be its own parent.");

        if (model.ParentFolderId != null && !await UserOwnsFolder(userId.Value, model.ParentFolderId.Value))
            return Forbid();

        folder.Name = name;
        folder.ParentFolderId = model.ParentFolderId;
        folder.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId.Value);
        return Ok(MapFolder(folder, noteCount));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var folder = await _context.NoteFolders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId.Value);
        if (folder == null) return NotFound();

        var notesInFolder = await _context.Notes
            .Where(n => n.FolderId == id && n.UserId == userId.Value)
            .ToListAsync();

        foreach (var note in notesInFolder)
            note.FolderId = null;

        var childFolders = await _context.NoteFolders
            .Where(f => f.ParentFolderId == id && f.UserId == userId.Value)
            .ToListAsync();

        foreach (var child in childFolders)
        {
            child.ParentFolderId = null;
            child.UpdatedAt = DateTime.UtcNow;
        }

        _context.NoteFolders.Remove(folder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // -------------------------
    // AUTH AND OWNERSHIP HELPERS
    // -------------------------
    private async Task<bool> UserOwnsFolder(int userId, int folderId)
    {
        return await _context.NoteFolders.AnyAsync(f => f.Id == folderId && f.UserId == userId);
    }

    private int? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;

        var userIdClaim = User.FindFirst("userId") ??
                          User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst("UserId") ??
                          User.FindFirst("user_id");

        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private static NoteFolderModel MapFolder(NoteFolder folder, int noteCount)
    {
        return new NoteFolderModel
        {
            Id = folder.Id,
            Name = folder.Name,
            UserId = folder.UserId,
            ParentFolderId = folder.ParentFolderId,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
            NoteCount = noteCount
        };
    }
}

// -------------------------
// REQUEST / RESPONSE MODELS
// -------------------------
public class NoteFolderSaveModel
{
    public string? Name { get; set; }
    public int? ParentFolderId { get; set; }
}

public class NoteFolderModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? ParentFolderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NoteCount { get; set; }
}
