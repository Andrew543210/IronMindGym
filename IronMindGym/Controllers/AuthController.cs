using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Security;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IronMindGym.Controllers;

[AllowAnonymous]
public class AuthController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordHashUtility.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);
        TempData["Message"] = $"Welcome, {user.Username}!";

        var hasSetup = db.Users
            .Include(u => u.Benchmarks)
            .Any(u => u.Id == user.Id && u.Benchmarks != null);

        return hasSetup
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Setup");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Email = model.Email.Trim().ToLowerInvariant();
        model.Username = model.Username.Trim();
        if (db.Users.Any(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View(model);
        }

        if (db.Users.Any(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "This username is already taken.");
            return View(model);
        }

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = PasswordHashUtility.Hash(model.Password),
            Profile = new Profile { Age = model.Age, AccountCreatedAt = DateTime.UtcNow }
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            ModelState.AddModelError(string.Empty, "Email or username is already registered.");
            return View(model);
        }

        await SignInUserAsync(user, false);
        TempData["Message"] = "Registration successful. Complete your initial setup.";

        return RedirectToAction("Index", "Setup");
    }

    [HttpPost]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Message"] = "You have logged out.";
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUserAsync(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("SecurityStamp", user.SecurityStamp)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
                AllowRefresh = true
            });

    }
}
