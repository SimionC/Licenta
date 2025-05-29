using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class CourseWork
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? Deadline { get; set; }

    public virtual Course Course { get; set; } = null!;
}
