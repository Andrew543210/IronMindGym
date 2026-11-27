using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/nutrition")]
public class NutritionController(NutritionJournalService journal) : ApiController
{
    [HttpGet]
    public Task<NutritionDay> Day(DateOnly? date, CancellationToken cancellationToken) => journal.DayAsync(UserId, date, cancellationToken);

    [HttpPut("days/{date}")]
    public async Task<ActionResult> SaveDay(DateOnly date, NutritionEntryRequest request, CancellationToken cancellationToken)
    {
        await journal.SaveAsync(UserId, date, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("days/{date}")]
    public async Task<ActionResult> DeleteDay(DateOnly date, CancellationToken cancellationToken) =>
        await journal.DeleteAsync(UserId, date, cancellationToken) ? NoContent() : NotFound();
}
