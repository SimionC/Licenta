using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class Grade
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int Grade1 { get; set; }

    public virtual ICollection<SubmittedWork> SubmittedWorks { get; set; } = new List<SubmittedWork>();
}
