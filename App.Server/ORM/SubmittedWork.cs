using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class SubmittedWork
{
    public int Id { get; set; }

    public int NoteId { get; set; }

    public int? GradeId { get; set; }

    public virtual Grade? Grade { get; set; }

    public virtual Note Note { get; set; } = null!;
}
