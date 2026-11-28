using System.Text.Json;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class WorkoutDraftService(IronMindDbContext db, TimeProvider clock)
{
    public const int MaxContentLength = 64_000;

    public async Task<WorkoutDraftResponse?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var draft = await db.WorkoutDrafts.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        if (draft is null)
            return null;
        using var content = JsonDocument.Parse(draft.Content);
        return new WorkoutDraftResponse(draft.DraftId, content.RootElement.Clone(), draft.UpdatedAt);
    }

    // Saving a workout takes the same lock and deletes the draft, so a late update for a saved workout is refused.
    public async Task SaveAsync(int userId, Guid draftId, JsonElement content, CancellationToken cancellationToken)
    {
        if (content.ValueKind != JsonValueKind.Object)
            throw new DomainValidationException("Send the unfinished workout as a JSON object.", nameof(WorkoutDraftRequest.Content));
        var json = content.GetRawText();
        if (json.Length > MaxContentLength)
            throw new DomainValidationException("This workout is too large to keep on the server.", nameof(WorkoutDraftRequest.Content));
        // PostgreSQL jsonb rejects the NUL character.
        if (json.Contains("\\u0000", StringComparison.Ordinal))
            throw new DomainValidationException("The workout contains a character that cannot be stored.", nameof(WorkoutDraftRequest.Content));

        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (await db.WorkoutSessions.AnyAsync(s => s.UserId == userId && s.DraftId == draftId, cancellationToken))
            throw new DomainConflictException("This workout is already saved.");
        var draft = await db.WorkoutDrafts.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        if (draft is null)
            db.WorkoutDrafts.Add(draft = new WorkoutDraft { UserId = userId });
        draft.DraftId = draftId;
        draft.Content = json;
        draft.UpdatedAt = clock.UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task DiscardAsync(int userId, Guid draftId, CancellationToken cancellationToken) =>
        db.WorkoutDrafts.Where(d => d.UserId == userId && d.DraftId == draftId).ExecuteDeleteAsync(cancellationToken);
}
