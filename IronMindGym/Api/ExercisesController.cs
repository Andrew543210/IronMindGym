using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/exercises")]
public class ExercisesController(ExerciseCatalog catalog) : ApiController
{
    [HttpGet]
    public Task<ExerciseLibraryResponse> Library(CancellationToken cancellationToken) => catalog.LibraryAsync(cancellationToken);
}
