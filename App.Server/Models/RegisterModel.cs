using System;
using System.Collections.Generic;

namespace App.Server.Models;

public partial class RegisterModel
{
    public string Email { get; set; } = null!;
    public string Nume { get; set; } = null!;
    public string Prenume { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? StudentId { get; set; }
    public int UserTypeId { get; set; }
}
