namespace App.Server.Models;

public class AccountModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public int UserTypeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}

public class CreatedAccountModel
{
    public AccountModel User { get; set; } = new();
    public string TemporaryPassword { get; set; } = string.Empty;
}

public class ChangePasswordModel
{
    public string NewPassword { get; set; } = string.Empty;
}
