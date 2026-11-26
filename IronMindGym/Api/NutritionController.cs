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
