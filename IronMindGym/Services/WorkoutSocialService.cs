using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Services;

public class WorkoutSocialService(IronMindDbContext db, FriendshipService friendships)
{
    public const int MaxCommentLength = 500;

    public bool SetLike(int viewerId, int ownerId, int sessionId, bool like)
    {
        if (viewerId == ownerId || !friendships.AreFriends(viewerId, ownerId) || !SessionExists(ownerId, sessionId))
            return false;
        using var transaction = WriteLock.BeginForAthletes(db, viewerId);
        var existing = db.ActivityLikes.FirstOrDefault(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId);
        if (like && existing is null)
            db.ActivityLikes.Add(new ActivityLike { WorkoutSessionId = sessionId, UserId = viewerId, CreatedAt = DateTime.UtcNow });
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
            throw new ValidationException($"Comments are limited to {MaxCommentLength} characters.");
        if (comment.Length == 0)
            return true;
        db.FriendComments.Add(new FriendComment { WorkoutSessionId = sessionId, AuthorId = viewerId, Text = comment, Date = DateTime.UtcNow });
        db.SaveChanges();
        return true;
    }

    private bool SessionExists(int ownerId, int sessionId) =>
        db.WorkoutSessions.Any(s => s.Id == sessionId && s.UserId == ownerId);
}
