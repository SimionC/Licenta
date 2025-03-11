using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class Note
{
    public int Id { get; set; }

    public string Guid { get; set; } = null!;

    public string Text { get; set; } = null!;

    public string? CreationDate { get; set; }

    public string? ModifyDate { get; set; }

    public int UserId { get; set; }

    public int VisibilityTypeId { get; set; }

    public virtual ICollection<CoursesNote> CoursesNotes { get; set; } = new List<CoursesNote>();

    public virtual ICollection<SubmittedWork> SubmittedWorks { get; set; } = new List<SubmittedWork>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UsersNote> UsersNotes { get; set; } = new List<UsersNote>();

    public virtual VisibilityType VisibilityType { get; set; } = null!;
}
