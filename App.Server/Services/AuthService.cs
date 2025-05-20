using App.Server.Models;
using App.Server.ORM;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace App.Server.Services;

public class AuthService
{
    private AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public bool Register(RegisterModel registerModel)
    {
        User? user = _dbContext.Users.Where(u => u.Email == registerModel.Email || u.StudentId == registerModel.StudentId).FirstOrDefault();
        if (user != null)
            return false; 

        user = new()
        {
            Email = registerModel.Email,
            Nume = registerModel.Nume,
            Prenume = registerModel.Prenume,
            StudentId = registerModel.StudentId,
            Password = SHA256.HashData(Encoding.UTF8.GetBytes(registerModel.Password)).ToString(),
            UserTypeId = registerModel.UserTypeId
        };

        _dbContext.Add(user);
        _dbContext.SaveChanges();

        return true;
    }    
    
    public RegisterModel? Login(LoginModel loginModel)
    {
        User? user = _dbContext.Users.Where(u => u.Email.ToLower() == loginModel.Email.ToLower()).FirstOrDefault();

        if (user == null)
            return null;

        string passwordHash = SHA256.HashData(Encoding.UTF8.GetBytes(loginModel.Password)).ToString();

        if (passwordHash == user.Password)
            return new RegisterModel()
            {
                Email = user.Email,
                Nume = user.Nume,
                Prenume = user.Prenume, 
                StudentId = user.StudentId,
                UserTypeId = user.UserTypeId
            };

        return null; 
    }
}
