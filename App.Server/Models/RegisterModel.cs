using System;
using System.Collections.Generic;

namespace App.Server.Models;

// Purpose: registration payload + reused shape for returning authenticated profile fields.
// Contract: userTypeId controls student/teacher role behavior.

public partial class RegisterModel
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Nume { get; set; } = null!;
    public string Prenume { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? StudentId { get; set; }
    public int UserTypeId { get; set; }
}
