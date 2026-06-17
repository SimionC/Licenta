using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

/// Purpose: downloadable teacher-managed files attached to a course.
/// Access: teacher owner may upload/remove; enrolled students may download.
public class CourseResource
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    public DateTime UploadedAt { get; set; }

    [ForeignKey("CourseId")]
    public Course Course { get; set; } = null!;
}
