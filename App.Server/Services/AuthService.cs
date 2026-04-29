using App.Server.Models;
using App.Server.ORM;
using Microsoft.AspNetCore.Identity;

//Purpose: Registration/login business rules and credential checks.
//Inputs/Outputs: Accepts register/login DTOs; writes user records and returns profile model on successful login.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/ORM/User.cs.

namespace App.Server.Services;

public class AuthService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();
    private AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public RegisterModel? Register(RegisterModel registerModel)
    {
        User? user = _dbContext.Users.Where(u => u.Email == registerModel.Email || u.StudentId == registerModel.StudentId).FirstOrDefault();
        if (user != null)
            return null; 

        user = new()
        {
            Email = registerModel.Email,
            Nume = registerModel.Nume,
            Prenume = registerModel.Prenume,
            StudentId = registerModel.StudentId,
            Password = string.Empty,
            UserTypeId = registerModel.UserTypeId
        };

        user.Password = PasswordHasher.HashPassword(user, registerModel.Password);

        _dbContext.Add(user);
        _dbContext.SaveChanges();

        return ToRegisterModel(user);
    }    
    
    public RegisterModel? Login(LoginModel loginModel)
    {
        User? user = _dbContext.Users.Where(u => u.Email.ToLower() == loginModel.Email.ToLower()).FirstOrDefault();

        if (user == null)
            return null;

        var verificationResult = PasswordHasher.VerifyHashedPassword(user, user.Password, loginModel.Password);

        if (verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            return ToRegisterModel(user);

        return null; 
    }

    private static RegisterModel ToRegisterModel(User user)
    {
        return new RegisterModel
        {
            Id = user.Id,
            Email = user.Email,
            Nume = user.Nume,
            Prenume = user.Prenume,
            Password = string.Empty,
            StudentId = user.StudentId,
            UserTypeId = user.UserTypeId
        };
    }
}
