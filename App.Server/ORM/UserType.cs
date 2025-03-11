using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class UserType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Permissions { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
