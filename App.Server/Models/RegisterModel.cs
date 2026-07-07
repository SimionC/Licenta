using System;
using System.Collections.Generic;

namespace App.Server.Models;

// Purpose: used more for auth/session shape
// 

public partial class RegisterModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public int UserTypeId { get; set; }
    public bool MustChangePassword { get; set; }
}
