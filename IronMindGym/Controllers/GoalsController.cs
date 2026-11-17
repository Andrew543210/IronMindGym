using System.ComponentModel.DataAnnotations;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Controllers;

public class GoalsController(GoalService goals) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new GoalViewModel { Goals = goals.List(User.GetUserId()!.Value) });

    [HttpPost]
    public IActionResult Index(GoalViewModel model)
    {
        var userId = User.GetUserId()!.Value;
        if (ModelState.IsValid)
        {
            try
            {
                goals.Create(userId, model.Title, model.Deadline);
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        model.Goals = goals.List(userId);
        return View(model);
    }

    [HttpPost]
    public IActionResult Toggle(int id) => goals.Toggle(User.GetUserId()!.Value, id) ? RedirectToAction(nameof(Index)) : NotFound();

    [HttpPost]
    public IActionResult Delete(int id) => goals.Delete(User.GetUserId()!.Value, id) ? RedirectToAction(nameof(Index)) : NotFound();
}
