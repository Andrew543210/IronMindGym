using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutSocialService(IronMindDbContext db, TimeProvider clock, FriendshipService friendships, WorkoutService workouts)
{
    public const int MaxCommentLength = 500;

    /// <summary>A friend's workout with its likes and comments; null unless the two are friends and the workout is the friend's.</summary>
    public FriendWorkoutResponse? FriendWorkout(int viewerId, int friendId, int sessionId)
    {
        if (!friendships.AreFriends(viewerId, friendId) || workouts.Details(friendId, sessionId, viewerId) is not { } details)
            return null;
        return new FriendWorkoutResponse(db.Users.AsNoTracking().Where(u => u.Id == friendId).Select(u => u.Username).First(), details);
    }

    public bool SetLike(int viewerId, int ownerId, int sessionId, bool like)
    {
        if (viewerId == ownerId || !friendships.AreFriends(viewerId, ownerId) || !SessionExists(ownerId, sessionId))
            return false;
        using var transaction = WriteLock.BeginForAthletes(db, viewerId);
        var existing = db.ActivityLikes.FirstOrDefault(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId);
        if (like && existing is null)
            db.ActivityLikes.Add(new ActivityLike { WorkoutSessionId = sessionId, UserId = viewerId, CreatedAt = clock.UtcNow() });
        else if (!like && existing is not null)
            db.ActivityLikes.Remove(existing);
        db.SaveChanges();
        transaction.Commit();
        return true;
    }

    public bool AddComment(int viewerId, int ownerId, int sessionId, string? text)
    {
        if ((viewerId != ownerId && !friendships.AreFriends(viewerId, ownerId)) || !SessionExists(ownerId, sessionId))
            return false;
        var comment = (text ?? string.Empty).Trim();
        if (comment.Length > MaxCommentLength)
            throw new DomainValidationException($"Comments are limited to {MaxCommentLength} characters.");
        if (comment.Length == 0)
            return true;
        db.FriendComments.Add(new FriendComment { WorkoutSessionId = sessionId, AuthorId = viewerId, Text = comment, CreatedAt = clock.UtcNow() });
        db.SaveChanges();
        return true;
    }

    private bool SessionExists(int ownerId, int sessionId) =>
        db.WorkoutSessions.Any(s => s.Id == sessionId && s.UserId == ownerId);
}
