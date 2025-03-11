using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class VisibilityType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
