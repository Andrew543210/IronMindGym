using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Security;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class AuthController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
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
            ModelState.AddModelError(string.Empty, "Невірний email або пароль.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        TempData["Message"] = $"Вітаю, {user.Username}!";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (db.Users.Any(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Користувач з таким email вже існує.");
            return View(model);
        }

        if (db.Users.Any(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Такий username вже зайнято.");
            return View(model);
        }

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = PasswordHashUtility.Hash(model.Password),
            Profile = new Profile { Age = 18, Gender = "Unknown", AccountCreatedAt = DateTime.UtcNow }
        };

        db.Users.Add(user);
        db.SaveChanges();

        HttpContext.Session.SetInt32("UserId", user.Id);
        TempData["Message"] = "Реєстрація успішна. Заповни стартові дані профілю.";

        return RedirectToAction("Index", "Setup");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("UserId");
        TempData["Message"] = "Ви вийшли з акаунту.";
        return RedirectToAction(nameof(Login));
    }
}
