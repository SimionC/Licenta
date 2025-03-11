using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Nume { get; set; } = null!;

    public string Prenume { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? StudentId { get; set; }

    public int UserTypeId { get; set; }

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual UserType UserType { get; set; } = null!;

    public virtual ICollection<UsersCourse> UsersCourses { get; set; } = new List<UsersCourse>();

    public virtual ICollection<UsersNote> UsersNotes { get; set; } = new List<UsersNote>();
}
