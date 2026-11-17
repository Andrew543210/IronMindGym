using System.ComponentModel.DataAnnotations;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Controllers;

public class NutritionController(NutritionJournalService journal) : Controller
{
    [HttpGet]
    public IActionResult Index(DateTime? date) => View(journal.Build(User.GetUserId()!.Value, date ?? DateTime.UtcNow.Date));

    [HttpPost]
    public IActionResult Index(NutritionViewModel model)
    {
        var userId = User.GetUserId()!.Value;
        if (ModelState.IsValid)
        {
            try
            {
                journal.Save(userId, model);
                TempData["Message"] = "Nutrition entry saved.";
                return RedirectToAction(nameof(Index), new
                {
                    date = model.Date.ToString("yyyy-MM-dd")
                });
            }
            catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        journal.Populate(userId, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var model = journal.Details(User.GetUserId()!.Value, id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        if (!journal.Delete(User.GetUserId()!.Value, id))
            return NotFound();
        TempData["Message"] = "Nutrition entry deleted.";
        return RedirectToAction(nameof(Index));
    }
}
