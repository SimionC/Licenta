using App.Server.Models;
using App.Server.ORM;
using App.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    private AuthService _authService; 

    public AuthController(AuthService authService   )
    {
        _authService = authService;
    }

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
            new Claim("StudentId", result.StudentId ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return Ok();
    }
}
