using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutSocialService(IronMindDbContext db, TimeProvider clock, FriendshipService friendships, WorkoutService workouts)
{
    public const int MaxCommentLength = 500;

    /// <summary>A friend's workout with its likes and comments; null unless the two are friends and the workout is the friend's.</summary>
    public async Task<FriendWorkoutResponse?> FriendWorkoutAsync(int viewerId, int friendId, int sessionId, CancellationToken cancellationToken)
    {
        if (!await friendships.AreFriendsAsync(viewerId, friendId, cancellationToken))
            return null;
        var details = await workouts.DetailsAsync(friendId, sessionId, viewerId, cancellationToken);
        if (details is null)
            return null;
        var username = await db.Users.AsNoTracking().Where(u => u.Id == friendId).Select(u => u.Username).FirstAsync(cancellationToken);
        return new FriendWorkoutResponse(username, details);
    }

    public async Task<bool> SetLikeAsync(int viewerId, int ownerId, int sessionId, bool like, CancellationToken cancellationToken)
    {
        // Athletes like their friends' workouts, not their own.
        var mayLike = viewerId != ownerId && await friendships.AreFriendsAsync(viewerId, ownerId, cancellationToken);
        if (!mayLike || !await SessionExistsAsync(ownerId, sessionId, cancellationToken))
            return false;
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, viewerId, cancellationToken);
        var existing = await db.ActivityLikes.FirstOrDefaultAsync(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId, cancellationToken);
        if (like && existing is null)
            db.ActivityLikes.Add(new ActivityLike { WorkoutSessionId = sessionId, UserId = viewerId, CreatedAt = clock.UtcNow() });
        else if (!like && existing is not null)
            db.ActivityLikes.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddCommentAsync(int viewerId, int ownerId, int sessionId, string? text, CancellationToken cancellationToken)
    {
        var mayComment = viewerId == ownerId || await friendships.AreFriendsAsync(viewerId, ownerId, cancellationToken);
        if (!mayComment || !await SessionExistsAsync(ownerId, sessionId, cancellationToken))
            return false;
        var comment = (text ?? string.Empty).Trim();
        if (comment.Length > MaxCommentLength)
            throw new DomainValidationException($"Comments are limited to {MaxCommentLength} characters.", nameof(CommentRequest.Text));
        if (comment.Length == 0)
            throw new DomainValidationException("Write a comment first.", nameof(CommentRequest.Text));
        db.FriendComments.Add(new FriendComment { WorkoutSessionId = sessionId, AuthorId = viewerId, Text = comment, CreatedAt = clock.UtcNow() });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<bool> SessionExistsAsync(int ownerId, int sessionId, CancellationToken cancellationToken) =>
        db.WorkoutSessions.AnyAsync(s => s.Id == sessionId && s.UserId == ownerId, cancellationToken);
}
