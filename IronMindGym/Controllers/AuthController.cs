using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Security;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IronMindGym.Controllers;

public class AuthController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = db.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.Email == model.Email);

        if (user is null || !PasswordHashUtility.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInUserAsync(user.Id, user.Username, model.RememberMe);
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
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

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
            Profile = new Profile { Age = model.Age, Gender = "Unknown", AccountCreatedAt = DateTime.UtcNow }
        };

        db.Users.Add(user);
        db.SaveChanges();

        await SignInUserAsync(user.Id, user.Username, true);
        TempData["Message"] = "Registration successful. Complete your initial setup.";

        return RedirectToAction("Index", "Setup");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove("UserId");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Message"] = "You have logged out.";
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUserAsync(int userId, string username, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username)
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

        HttpContext.Session.SetInt32("UserId", userId);
    }
}
