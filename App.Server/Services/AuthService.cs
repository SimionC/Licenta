using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Identity; //password safety
using System.Security.Cryptography;  //random generation

//Purpose:-logic layer- does the actual account work: creating users, checking passwords, hashing passwords, resetting passwords, updating profiles, and reading users from the database.
//Inputs/Outputs: Accepts register/login DTOs; writes user records and returns profile model on successful login.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/ORM/User.cs.
//Works hand in hand with AuthController(API layer - it receives the HTTP req)

namespace App.Server.Services;

public class AuthService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();
    private const string TemporaryPasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"; //no 0,O,I,1 misidentification purposes(my tragedy all these years)
    private readonly AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public CreatedAccountModel? CreateAccount(RegisterModel registerModel)
    {
        if (!UserRoles.IsKnownRole(registerModel.UserTypeId))
            return null;

        var email = registerModel.Email.Trim();
        var studentId = string.IsNullOrWhiteSpace(registerModel.StudentId)
            ? null
            : registerModel.StudentId.Trim();

        var user = _dbContext.Users
            .Where(u => u.Email.ToLower() == email.ToLower() ||
                (studentId != null && u.StudentId == studentId))
            .FirstOrDefault();
        if (user != null)
            return null;

        var temporaryPassword = GenerateTemporaryPassword();

        user = new()
        {
            Email = email,
            Nume = registerModel.Nume.Trim(),
            Prenume = registerModel.Prenume.Trim(),
            StudentId = studentId,
            Password = string.Empty,
            UserTypeId = registerModel.UserTypeId,
            MustChangePassword = true
        };

        user.Password = PasswordHasher.HashPassword(user, temporaryPassword);

        _dbContext.Add(user);
        _dbContext.SaveChanges();

        return new CreatedAccountModel
        {
            User = ToAccountModel(user),
            TemporaryPassword = temporaryPassword
        };
    }

    public RegisterModel? Login(LoginModel loginModel)
    {
        var email = loginModel.Email.Trim();
        var user = _dbContext.Users
            .Where(u => u.Email.ToLower() == email.ToLower())
            .FirstOrDefault();

        if (user == null)
            return null;

        var verificationResult = PasswordHasher.VerifyHashedPassword(user, user.Password, loginModel.Password);

        if (verificationResult == PasswordVerificationResult.Success || 
            verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            return ToRegisterModel(user);

        return null;
    }

    public RegisterModel? GetProfileById(int userId)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        return user == null ? null : ToRegisterModel(user);
    }

 
    public AccountModel? GetAccountProfileById(int userId)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        return user == null ? null : ToAccountModel(user);
    }

    public AccountModel? UpdateProfile(int userId, UpdateProfileModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Nume) || string.IsNullOrWhiteSpace(model.Prenume))
            return null;

        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return null;

        user.Nume = model.Nume.Trim();
        user.Prenume = model.Prenume.Trim();
        _dbContext.SaveChanges();

        return ToAccountModel(user);
    }

    public List<AccountModel> GetUsers()
    {
        return _dbContext.Users
            .OrderBy(u => u.Nume)
            .ThenBy(u => u.Prenume)
            .ThenBy(u => u.Email)
            .Select(u => ToAccountModel(u))
            .ToList();
    }

    public CreatedAccountModel? ResetPassword(int userId)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return null;

        var temporaryPassword = GenerateTemporaryPassword();
        user.Password = PasswordHasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        _dbContext.SaveChanges();

        return new CreatedAccountModel
        {
            User = ToAccountModel(user),
            TemporaryPassword = temporaryPassword
        };
    }

    public bool ChangePassword(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return false;

        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return false;

        user.Password = PasswordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        _dbContext.SaveChanges();
        return true;
    }

    private static RegisterModel ToRegisterModel(User user) //converts database User into RegisterModel
    {
        return new RegisterModel
        {
            Id = user.Id,
            Email = user.Email,
            Nume = user.Nume,
            Prenume = user.Prenume,
            Password = string.Empty,
            StudentId = user.StudentId,
            UserTypeId = user.UserTypeId,
            MustChangePassword = user.MustChangePassword
        };
    }

    private static AccountModel ToAccountModel(User user) //converts database User into AccountModel
    {
        return new AccountModel
        {
            Id = user.Id,
            Email = user.Email,
            Nume = user.Nume,
            Prenume = user.Prenume,
            StudentId = user.StudentId,
            UserTypeId = user.UserTypeId,
            UserType = UserRoles.GetName(user.UserTypeId),
            Role = UserRoles.GetName(user.UserTypeId),
            MustChangePassword = user.MustChangePassword
        };
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Temp-{RandomNumberGenerator.GetString(TemporaryPasswordCharacters, 10)}!";
    }
}
