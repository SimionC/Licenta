using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class UsersNote
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int NoteId { get; set; }

    public string? PermissionLevel { get; set; }

    public virtual Note Note { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
