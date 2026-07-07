using App.Server.Models;
using App.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.Server.Controllers;

//Purpose: exposes API endpoints for login, logout, current user info, password changes, profile updates, and admin account management
//Inputs/Outputs: Receives account/login/password DTOs/models; sets or clears cookie sessions
//Depends on: App.Server/Services/AuthService.cs and cookie auth configured in App.Server/Program.cs

[ApiController] 
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    //The controller receives AuthService automatically from dependency injection
    //Dependency Injection reduces the hard-coded dependencies among your classes by injecting those dependencies at run time instead of design time technically.
    private readonly AuthService _authService;
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [Authorize]
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        return Ok(_authService.GetUsers());
    }

    [Authorize]
    [HttpPost("users")]
    public IActionResult CreateUser(RegisterModel registerModel)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        if (!UserRoles.IsKnownRole(registerModel.UserTypeId))
            return BadRequest("Unsupported role.");

        if (string.IsNullOrWhiteSpace(registerModel.Email) ||
            string.IsNullOrWhiteSpace(registerModel.Nume) ||
            string.IsNullOrWhiteSpace(registerModel.Prenume))
            return BadRequest("First name, last name, and email are required.");

        if (UserRoles.IsStudent(registerModel.UserTypeId) && string.IsNullOrWhiteSpace(registerModel.StudentId))
            return BadRequest("Student ID is required for student accounts.");

        if (!UserRoles.IsStudent(registerModel.UserTypeId))
            registerModel.StudentId = null;

        var result = _authService.CreateAccount(registerModel);

        if (result == null)
            return BadRequest("Account could not be created. Check role, email, and student id uniqueness.");

        return Ok(result);
    }

    [Authorize]
    [HttpPost("users/{id:int}/reset-password")]
    public IActionResult ResetPassword(int id)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var result = _authService.ResetPassword(id);
        return result == null ? NotFound() : Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        var result = _authService.Login(loginModel);

        if (result == null)
            return BadRequest();

        var claims = CreateClaims(result);

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return Ok();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var changed = _authService.ChangePassword(userId.Value, model.NewPassword);
        if (!changed)
            return BadRequest("Password must be at least 8 characters.");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var profile = _authService.GetAccountProfileById(userId.Value);
        if (profile == null)
            return Unauthorized();

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public IActionResult UpdateProfile(UpdateProfileModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var profile = _authService.UpdateProfile(userId.Value, model);
        if (profile == null)
            return BadRequest("First name and last name are required.");

        return Ok(profile);
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var profile = _authService.GetProfileById(userId.Value);
        if (profile == null)
            return Unauthorized();

        return Ok(new
        {
            id = profile.Id,
            name = profile.Nume,
            lastName = profile.Prenume,
            email = profile.Email,
            userType = UserRoles.GetName(profile.UserTypeId),
            userTypeId = profile.UserTypeId,
            studentId = profile.StudentId,
            mustChangePassword = profile.MustChangePassword
        });
    }

    private bool IsCurrentUserAdmin()
    {
        var userTypeId = GetCurrentUserTypeId();
        return userTypeId != null && UserRoles.IsAdmin(userTypeId.Value);
    }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        return int.TryParse(idClaim, out var userId) ? userId : null;
    }

    private int? GetCurrentUserTypeId()
    {
        var roleClaim = User.FindFirst("UserTypeId")?.Value;
        return int.TryParse(roleClaim, out var roleId) ? roleId : null;
    }

    private static List<Claim> CreateClaims(RegisterModel user)
    {
        return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Nume),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Name", user.Nume),
            new Claim("LastName", user.Prenume),
            new Claim("Email", user.Email),
            new Claim("UserTypeId", user.UserTypeId.ToString()),
            new Claim("StudentId", user.StudentId ?? string.Empty),
            new Claim("MustChangePassword", user.MustChangePassword.ToString()),
            new Claim("userId", user.Id.ToString())
        };
    }
}
