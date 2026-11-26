using System.ComponentModel.DataAnnotations;
using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/nutrition")]
public class NutritionController(NutritionJournalService journal, AthleteCalendars calendars) : ApiController
{
    [HttpGet]
    public NutritionDay Day(DateOnly? date) =>
        journal.Day(UserId, date ?? calendars.For(UserId).Today);

    [HttpPut("days/{date}")]
    public ActionResult SaveDay(DateOnly date, NutritionEntryRequest request)
    {
        journal.Save(UserId, date, request.CaloriesKcal, request.ProteinG, request.FatG, request.CarbsG);
        return NoContent();
    }

    [HttpDelete("days/{date}")]
    public ActionResult DeleteDay(DateOnly date) =>
        journal.Delete(UserId, date) ? NoContent() : NotFound();
}

public sealed record NutritionEntryRequest(
    [Range(0, 10000, ErrorMessage = "Calories must be 0–10000 kcal.")] double CaloriesKcal,
    [Range(0, 500, ErrorMessage = "Protein must be 0–500 g.")] double ProteinG,
    [Range(0, 300, ErrorMessage = "Fat must be 0–300 g.")] double FatG,
    [Range(0, 1000, ErrorMessage = "Carbs must be 0–1000 g.")] double CarbsG);
