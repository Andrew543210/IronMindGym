using System.Text.Json;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>The server copy of the athlete's unfinished workout, so it can be continued on another device.</summary>
public sealed class WorkoutDraftService(IronMindDbContext db, TimeProvider clock)
{
    public const int MaxContentLength = 64_000;

    public WorkoutDraftResponse? Get(int userId)
    {
        var draft = db.WorkoutDrafts.AsNoTracking().FirstOrDefault(d => d.UserId == userId);
        if (draft is null)
            return null;
        using var content = JsonDocument.Parse(draft.Content);
        return new WorkoutDraftResponse(draft.DraftId, content.RootElement.Clone(), draft.UpdatedAt);
    }

    /// <summary>
    /// Saving a workout takes the same athlete lock and deletes its draft, so a late request
    /// for an already saved workout is refused instead of bringing the draft back.
    /// </summary>
    public void Save(int userId, Guid draftId, JsonElement content)
    {
        if (content.ValueKind != JsonValueKind.Object)
            throw new DomainValidationException("Send the unfinished workout as a JSON object.", "Content");
        var json = content.GetRawText();
        if (json.Length > MaxContentLength)
            throw new DomainValidationException("This workout is too large to keep on the server.", "Content");
        // PostgreSQL jsonb rejects the NUL character.
        if (json.Contains("\\u0000", StringComparison.Ordinal))
            throw new DomainValidationException("The workout contains a character that cannot be stored.", "Content");

        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (db.WorkoutSessions.Any(s => s.UserId == userId && s.DraftId == draftId))
            throw new DomainConflictException("This workout is already saved.");
        var draft = db.WorkoutDrafts.FirstOrDefault(d => d.UserId == userId);
        if (draft is null)
            db.WorkoutDrafts.Add(draft = new WorkoutDraft { UserId = userId });
        draft.DraftId = draftId;
        draft.Content = json;
        draft.UpdatedAt = clock.UtcNow();
        db.SaveChanges();
        transaction.Commit();
    }

    public void Discard(int userId, Guid draftId) =>
        db.WorkoutDrafts.Where(d => d.UserId == userId && d.DraftId == draftId).ExecuteDelete();
}
