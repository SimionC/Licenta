using App.Server.Models;
using App.Server.ORM;
using App.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.Server.Controllers;

//Purpose: Authentication endpoints (register, login, me) and cookie sign-in.
//Inputs/Outputs: Receives RegisterModel/LoginModel; sets cookie claims; returns lightweight identity payload.
//Depends on: App.Server/Services/AuthService.cs, cookie auth configured in App.Server/Program.cs.

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthController : ControllerBase
{
    private AuthService _authService; 

    public AuthController(AuthService authService   )
    {
        _authService = authService;
    }

    //Trigger: POST register.
    //Guards: Rejects when AuthService says existing user.
    //Actions: Builds claims, signs in cookie principal.
    //Result: 200 OK + active session.
    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel registerModel)
    {
        var result = _authService.Register(registerModel);

        if (!result)
            return BadRequest();

        var claims = new List<Claim>
        {
            new Claim("Name", registerModel.Nume),
            new Claim("LastName", registerModel.Prenume),
            new Claim("Email", registerModel.Email),
            new Claim("UserTypeId", registerModel.UserTypeId.ToString()),
            new Claim("StudentId", registerModel.StudentId ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return Ok();
    }    
    
    [HttpPost]
    //Trigger: POST login.
    //Guards: Invalid credentials return bad request.
    //Actions: Builds claims including userId and role info, signs in.
    //Result: 200 OK + active session.
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        RegisterModel? result = _authService.Login(loginModel);

        if (result == null)
            return BadRequest();

        var claims = new List<Claim>
        {
            new Claim("Name", result.Nume),
            new Claim("LastName", result.Prenume),
            new Claim("Email", result.Email),
            new Claim("UserTypeId", result.UserTypeId.ToString()),
            new Claim("StudentId", result.StudentId ?? string.Empty),
            new Claim("userId", result.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return Ok();
    }

    [HttpGet]
    //Trigger: GET current user.
    //Guards: Requires authenticated identity.
    //Actions: Reads claims Name/Email/UserTypeId.
    //Result: Returns name/email/userType (teacher/student).
    public IActionResult Me()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var name = User.FindFirst("Name")?.Value;
        var email = User.FindFirst("Email")?.Value;
        var userTypeId = User.FindFirst("UserTypeId")?.Value;

        return Ok(new
        {
            name,
            email,
            userType = userTypeId == "2" ? "teacher" : "student"
        });
    }


}
