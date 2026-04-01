using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;

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

        var user = db.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Невірний email або пароль.");
            return View(model);
        }

        TempData["Message"] = $"Вітаю, {user.Username}! Для лаби використовується демо-сесія.";
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

        if (db.Users.Any(u => u.Email == model.Email || u.Username == model.Username))
        {
            ModelState.AddModelError(string.Empty, "Користувач з таким email або username вже існує.");
            return View(model);
        }

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = model.Password,
            Profile = new Profile { Age = 18, Gender = "Unknown", AccountCreatedAt = DateTime.UtcNow }
        };

        db.Users.Add(user);
        db.SaveChanges();

        TempData["Message"] = "Реєстрація успішна. Можна перейти до стартового налаштування.";
        return RedirectToAction("Index", "Setup");
    }
}

