using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class CoursesNote
{
    public int Id { get; set; }

    public int NoteId { get; set; }

    public int CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Note Note { get; set; } = null!;
}
