using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

/// Purpose: user-owned personal note folder. ParentFolderId is reserved for future nesting.
public class NoteFolder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    public int? ParentFolderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("ParentFolderId")]
    public NoteFolder? ParentFolder { get; set; }

    public ICollection<NoteFolder> ChildFolders { get; set; } = new List<NoteFolder>();

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
