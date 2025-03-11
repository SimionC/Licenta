using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class UsersCourse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
