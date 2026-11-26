using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class FriendshipService(IronMindDbContext db, AthleteCalendars calendars, AchievementService achievements)
{
    public bool AreFriends(int userId, int otherId) => db.Friendships.AsNoTracking().Any(f =>
        f.User1Id == Math.Min(userId, otherId) && f.User2Id == Math.Max(userId, otherId) && f.Status == FriendshipStatus.Accepted);

    public FriendsOverview BuildIndex(int userId, string? searchUsername)
    {
        var search = (searchUsername ?? "").Trim();
        var relationships = db.Friendships.AsNoTracking()
            .Where(f => f.User1Id == userId || f.User2Id == userId).ToList();
        var relatedIds = relationships.Select(f => f.User1Id == userId ? f.User2Id : f.User1Id).ToList();
        var users = db.Users.AsNoTracking().Where(u => u.Id != userId);
        users = search.Length == 0 ? users.Where(u => relatedIds.Contains(u.Id)) : users.Where(u => EF.Functions.ILike(u.Username, $"%{EscapeLike(search)}%"));
        var firstNutritionDay = calendars.For(userId).Today.AddDays(-9);
        var cards = users.OrderBy(u => u.Username).Take(20).Select(u => new { u.Id, u.Username }).AsEnumerable().Select(u =>
        {
            var relationship = relationships.FirstOrDefault(f => f.User1Id == u.Id || f.User2Id == u.Id);
            var pending = relationship?.Status == FriendshipStatus.Pending;
            var card = new FriendCard
            {
                UserId = u.Id,
                Username = u.Username,
                IsFriend = relationship?.Status == FriendshipStatus.Accepted,
                RequestReceived = pending && relationship!.RequestedByUserId != userId,
                RequestSent = pending && relationship!.RequestedByUserId == userId
            };
            return card.IsFriend ? WithPreviews(card, firstNutritionDay) : card;
        }).ToList();
        return new FriendsOverview { Friends = cards };
    }

    private const int PreviewSize = 5;

    /// <summary>
    /// A friend's card shows their latest training, nutrition, records and awards. At most 20 cards are shown, and
    /// each preview is a small indexed LIMIT query, which stays bounded however long a friend's history grows.
    /// </summary>
    private FriendCard WithPreviews(FriendCard card, DateOnly firstNutritionDay)
    {
        var workouts = db.WorkoutSessions.AsNoTracking().Where(s => s.UserId == card.UserId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Id).Take(PreviewSize)
            .Select(s => new FriendWorkoutPreview { WorkoutSessionId = s.Id, Name = WorkoutService.SessionName(s.SplitDay!.WorkoutName, s.SplitDay.WorkoutNameUk), SessionDate = s.SessionDate, DurationMinutes = s.DurationMinutes })
            .ToList();
        var nutrition = db.DailyNutritions.AsNoTracking().Where(n => n.UserId == card.UserId && n.Date >= firstNutritionDay)
            .OrderByDescending(n => n.Date)
            .Select(n => new FriendNutritionPreview { Date = n.Date, CaloriesKcal = n.CaloriesKcal, ProteinG = n.ProteinG, FatG = n.FatG, CarbsG = n.CarbsG })
            .ToList();
        return card with
        {
            LastFiveWorkouts = workouts,
            LastFiveWorkoutsCount = workouts.Count,
            LastNutritionLogs = nutrition,
            AverageKcal = nutrition.Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average(),
            PersonalRecords = db.PersonalRecords.AsNoTracking().Where(r => r.UserId == card.UserId)
                .OrderByDescending(r => r.AchievedAt).Take(PreviewSize)
                .Select(r => new FriendRecordPreview { ExerciseName = Localized.Pick(r.Exercise.Name, r.Exercise.NameUk), MaxWeightKg = r.MaxWeightKg, AchievedAt = r.AchievedAt })
                .ToList(),
            Achievements = db.UserAchievements.AsNoTracking().Where(a => a.UserId == card.UserId)
                .OrderByDescending(a => a.AchievedAt).Take(PreviewSize)
                .Select(a => new FriendAchievementPreview
                {
                    Name = Localized.Pick(a.Achievement.Name, a.Achievement.NameUk),
                    ConditionDescription = Localized.Pick(a.Achievement.ConditionDescription, a.Achievement.ConditionDescriptionUk),
                    AchievedAt = a.AchievedAt
                })
                .ToList()
        };
    }

    public enum RequestResult { Sent, AlreadyRequested, Connected, AlreadyConnected }

    /// <summary>
    /// Sends a request, or accepts the other user's pending request when both want to connect.
    /// The lock serializes a double tap, so the second request sees the first one's row.
    /// </summary>
    public RequestResult SendRequest(int userId, int otherId)
    {
        if (userId == otherId || !db.Users.Any(u => u.Id == otherId))
            throw new DomainValidationException("Choose another registered user.");
        using var transaction = WriteLock.BeginForAthletes(db, userId, otherId);
        var relationship = Find(userId, otherId);
        RequestResult result;
        if (relationship is null)
        {
            db.Friendships.Add(new Friendship { User1Id = Math.Min(userId, otherId), User2Id = Math.Max(userId, otherId), RequestedByUserId = userId });
            result = RequestResult.Sent;
        }
        else if (relationship.Status == FriendshipStatus.Accepted)
        {
            result = RequestResult.AlreadyConnected;
        }
        else if (relationship.RequestedByUserId == userId)
        {
            result = RequestResult.AlreadyRequested;
        }
        else
        {
            relationship.Status = FriendshipStatus.Accepted;
            result = RequestResult.Connected;
        }
        db.SaveChanges();
        if (result == RequestResult.Connected)
            AssignFriendAwards(userId, otherId);
        transaction.Commit();
        return result;
    }

    public bool Accept(int userId, int otherId)
    {
        using var transaction = WriteLock.BeginForAthletes(db, userId, otherId);
        var relationship = Find(userId, otherId);
        if (relationship?.Status == FriendshipStatus.Accepted)
            return true;
        if (relationship is null || relationship.Status != FriendshipStatus.Pending || relationship.RequestedByUserId == userId)
            return false;
        relationship.Status = FriendshipStatus.Accepted;
        db.SaveChanges();
        AssignFriendAwards(userId, otherId);
        transaction.Commit();
        return true;
    }

    private void AssignFriendAwards(int userId, int otherId)
    {
        achievements.AwardEarned(userId);
        achievements.AwardEarned(otherId);
    }

    public bool Remove(int userId, int otherId) => db.Friendships
        .Where(f => f.User1Id == Math.Min(userId, otherId) && f.User2Id == Math.Max(userId, otherId))
        .ExecuteDelete() > 0;

    /// <summary>Usernames may contain "_", which LIKE would otherwise treat as a wildcard.</summary>
    private static string EscapeLike(string text) => text
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);

    private Friendship? Find(int userId, int otherId) => db.Friendships.FirstOrDefault(f =>
        f.User1Id == Math.Min(userId, otherId) && f.User2Id == Math.Max(userId, otherId));
}
