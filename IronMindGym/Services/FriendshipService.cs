using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class FriendshipService(IronMindDbContext db, AthleteCalendars calendars, AchievementService achievements)
{
    public Task<bool> AreFriendsAsync(int userId, int otherId, CancellationToken cancellationToken) =>
        Between(userId, otherId).AsNoTracking().AnyAsync(f => f.Status == FriendshipStatus.Accepted, cancellationToken);

    public async Task<FriendsOverview> BuildIndexAsync(int userId, string? searchUsername, CancellationToken cancellationToken)
    {
        var search = (searchUsername ?? "").Trim();
        var relationships = await db.Friendships.AsNoTracking()
            .Where(f => f.User1Id == userId || f.User2Id == userId).ToListAsync(cancellationToken);
        var relatedIds = relationships.Select(f => f.User1Id == userId ? f.User2Id : f.User1Id).ToList();
        var users = db.Users.AsNoTracking().Where(u => u.Id != userId);
        users = search.Length == 0 ? users.Where(u => relatedIds.Contains(u.Id)) : users.Where(u => EF.Functions.ILike(u.Username, $"%{EscapeLike(search)}%", LikeEscape));
        var found = await users.OrderBy(u => u.Username).Take(20).Select(u => new { u.Id, u.Username }).ToListAsync(cancellationToken);
        var firstNutritionDay = (await calendars.ForAsync(userId, cancellationToken)).Today.AddDays(-9);
        var cards = new List<FriendCard>(found.Count);
        foreach (var user in found)
        {
            var relationship = relationships.FirstOrDefault(f => f.User1Id == user.Id || f.User2Id == user.Id);
            var pending = relationship?.Status == FriendshipStatus.Pending;
            var card = new FriendCard
            {
                UserId = user.Id,
                Username = user.Username,
                IsFriend = relationship?.Status == FriendshipStatus.Accepted,
                RequestReceived = pending && relationship!.RequestedByUserId != userId,
                RequestSent = pending && relationship!.RequestedByUserId == userId
            };
            cards.Add(card.IsFriend ? await WithPreviewsAsync(card, firstNutritionDay, cancellationToken) : card);
        }
        return new FriendsOverview { Friends = cards };
    }

    private const int PreviewSize = 5;

    /// <summary>
    /// A friend's card shows their latest training, nutrition, records and awards. At most 20 cards are shown, and
    /// each preview is a small indexed LIMIT query, which stays bounded however long a friend's history grows.
    /// </summary>
    private async Task<FriendCard> WithPreviewsAsync(FriendCard card, DateOnly firstNutritionDay, CancellationToken cancellationToken)
    {
        var workouts = await db.WorkoutSessions.AsNoTracking().Where(s => s.UserId == card.UserId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Id).Take(PreviewSize)
            .Select(WorkoutService.ListItem)
            .ToListAsync(cancellationToken);
        var nutrition = await db.DailyNutritions.AsNoTracking().Where(n => n.UserId == card.UserId && n.Date >= firstNutritionDay)
            .OrderByDescending(n => n.Date)
            .Select(n => new NutritionLogEntry(n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG))
            .ToListAsync(cancellationToken);
        var records = await db.PersonalRecords.AsNoTracking().Where(r => r.UserId == card.UserId)
            .OrderByDescending(r => r.AchievedAt).Take(PreviewSize)
            .Select(r => new FriendRecordPreview { ExerciseName = Localized.Pick(r.Exercise.Name, r.Exercise.NameUk), MaxWeightKg = r.MaxWeightKg, AchievedAt = r.AchievedAt })
            .ToListAsync(cancellationToken);
        var awards = await db.UserAchievements.AsNoTracking().Where(a => a.UserId == card.UserId)
            .OrderByDescending(a => a.AchievedAt).Take(PreviewSize)
            .Select(a => new ProfileAchievement(
                Localized.Pick(a.Achievement.Name, a.Achievement.NameUk),
                Localized.Pick(a.Achievement.ConditionDescription, a.Achievement.ConditionDescriptionUk),
                a.AchievedAt))
            .ToListAsync(cancellationToken);
        return card with
        {
            LastFiveWorkouts = workouts,
            LastNutritionLogs = nutrition,
            AverageKcal = nutrition.Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average(),
            PersonalRecords = records,
            Achievements = awards
        };
    }


    /// <summary>
    /// Sends a request, or accepts the other user's pending request when both want to connect.
    /// The lock serializes a double tap, so the second request sees the first one's row.
    /// </summary>
    public async Task<FriendRequestResult> SendRequestAsync(int userId, int otherId, CancellationToken cancellationToken)
    {
        if (userId == otherId || !await db.Users.AnyAsync(u => u.Id == otherId, cancellationToken))
            throw new DomainValidationException("Choose another registered user.");
        await using var transaction = await WriteLock.BeginForAthletesAsync(db, [userId, otherId], cancellationToken);
        var relationship = await FindAsync(userId, otherId, cancellationToken);
        FriendRequestResult result;
        if (relationship is null)
        {
            db.Friendships.Add(new Friendship { User1Id = Math.Min(userId, otherId), User2Id = Math.Max(userId, otherId), RequestedByUserId = userId });
            result = FriendRequestResult.Sent;
        }
        else if (relationship.Status == FriendshipStatus.Accepted)
        {
            result = FriendRequestResult.AlreadyConnected;
        }
        else if (relationship.RequestedByUserId == userId)
        {
            result = FriendRequestResult.AlreadyRequested;
        }
        else
        {
            relationship.Status = FriendshipStatus.Accepted;
            result = FriendRequestResult.Connected;
        }
        await db.SaveChangesAsync(cancellationToken);
        if (result == FriendRequestResult.Connected)
            await AssignFriendAwardsAsync(userId, otherId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> AcceptAsync(int userId, int otherId, CancellationToken cancellationToken)
    {
        await using var transaction = await WriteLock.BeginForAthletesAsync(db, [userId, otherId], cancellationToken);
        var relationship = await FindAsync(userId, otherId, cancellationToken);
        if (relationship?.Status == FriendshipStatus.Accepted)
            return true;
        if (relationship is null || relationship.Status != FriendshipStatus.Pending || relationship.RequestedByUserId == userId)
            return false;
        relationship.Status = FriendshipStatus.Accepted;
        await db.SaveChangesAsync(cancellationToken);
        await AssignFriendAwardsAsync(userId, otherId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task AssignFriendAwardsAsync(int userId, int otherId, CancellationToken cancellationToken)
    {
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await achievements.AwardEarnedAsync(otherId, cancellationToken);
    }

    public async Task<bool> RemoveAsync(int userId, int otherId, CancellationToken cancellationToken) =>
        await Between(userId, otherId).ExecuteDeleteAsync(cancellationToken) > 0;

    /// <summary>
    /// Usernames may contain "_", which LIKE would otherwise treat as a wildcard. The escape character is named
    /// in the query, because Npgsql turns escaping off for a pattern that is not a constant.
    /// </summary>
    private const string LikeEscape = @"\";

    private static string EscapeLike(string text) => text
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    private Task<Friendship?> FindAsync(int userId, int otherId, CancellationToken cancellationToken) =>
        Between(userId, otherId).FirstOrDefaultAsync(cancellationToken);

    /// <summary>The row of two athletes, which always stores the lower id first.</summary>
    private IQueryable<Friendship> Between(int userId, int otherId)
    {
        var (first, second) = userId < otherId ? (userId, otherId) : (otherId, userId);
        return db.Friendships.Where(f => f.User1Id == first && f.User2Id == second);
    }
}
