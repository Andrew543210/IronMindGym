using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/workouts/draft")]
public class WorkoutDraftController(IronMindDbContext db) : ApiController
{
    public const int MaxContentLength = 64_000;

    [HttpGet]
    public ActionResult<WorkoutDraftResponse> Get()
    {
        var draft = db.WorkoutDrafts.AsNoTracking().FirstOrDefault(d => d.UserId == UserId);
        if (draft is null)
            return NoContent();
        using var content = JsonDocument.Parse(draft.Content);
        return new WorkoutDraftResponse(draft.DraftId, content.RootElement.Clone(), draft.UpdatedAt);
    }

    /// <summary>
    /// Saving a workout takes the same athlete lock and deletes its draft, so a late request
    /// for an already saved workout is refused instead of bringing the draft back.
    /// </summary>
    [HttpPut]
    [RequestSizeLimit(150_000)]
    public ActionResult Save(WorkoutDraftRequest request)
    {
        var content = request.Content!.Value;
        if (content.ValueKind != JsonValueKind.Object)
            throw new DomainValidationException("Send the unfinished workout as a JSON object.", nameof(request.Content));
        var json = content.GetRawText();
        if (json.Length > MaxContentLength)
            throw new DomainValidationException("This workout is too large to keep on the server.", nameof(request.Content));
        // PostgreSQL jsonb rejects the NUL character.
        if (json.Contains("\\u0000", StringComparison.Ordinal))
            throw new DomainValidationException("The workout contains a character that cannot be stored.", nameof(request.Content));
        var draftId = request.DraftId!.Value;

        using var transaction = WriteLock.BeginForAthletes(db, UserId);
        if (db.WorkoutSessions.Any(s => s.UserId == UserId && s.DraftId == draftId))
            throw new DomainConflictException("This workout is already saved.");
        var draft = db.WorkoutDrafts.FirstOrDefault(d => d.UserId == UserId);
        if (draft is null)
            db.WorkoutDrafts.Add(draft = new WorkoutDraft { UserId = UserId });
        draft.DraftId = draftId;
        draft.Content = json;
        draft.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
        transaction.Commit();
        return NoContent();
    }

    [HttpDelete]
    public ActionResult Discard(Guid draftId)
    {
        db.WorkoutDrafts.Where(d => d.UserId == UserId && d.DraftId == draftId).ExecuteDelete();
        return NoContent();
    }
}

public sealed record WorkoutDraftRequest([Required] Guid? DraftId, [Required] JsonElement? Content);

public sealed record WorkoutDraftResponse(Guid DraftId, JsonElement Content, DateTime UpdatedAt);
