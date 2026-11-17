using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class FriendshipService(IronMindDbContext db)
{
    public bool AreFriends(int userId, int otherId) => db.Friendships.AsNoTracking().Any(f =>
        f.Status == "Accepted" &&
        ((f.User1Id == userId && f.User2Id == otherId) || (f.User2Id == userId && f.User1Id == otherId)));

    public FriendsViewModel BuildIndex(int userId, string? searchUsername)
    {
        var search = (searchUsername ?? "").Trim();
        var relationships = db.Friendships.AsNoTracking()
            .Where(f => f.User1Id == userId || f.User2Id == userId).ToList();
        var relatedIds = relationships.Select(f => f.User1Id == userId ? f.User2Id : f.User1Id).ToList();
        var users = db.Users.AsNoTracking().Where(u => u.Id != userId);
        users = search.Length == 0 ? users.Where(u => relatedIds.Contains(u.Id)) : users.Where(u => u.Username.ToLower().Contains(search.ToLower()));
        var cards = users.OrderBy(u => u.Username).Take(20).Select(u => new FriendCardViewModel { UserId = u.Id, Username = u.Username }).ToList();
        foreach (var card in cards)
        {
            var relationship = relationships.FirstOrDefault(f => f.User1Id == card.UserId || f.User2Id == card.UserId);
            card.IsFriend = relationship?.Status == "Accepted";
            card.RequestReceived = relationship?.Status == "Pending" && relationship.RequestedByUserId != userId;
            card.RequestSent = relationship?.Status == "Pending" && relationship.RequestedByUserId == userId;
            card.Status = card.IsFriend ? "Friend" : card.RequestReceived ? "Wants to connect" : card.RequestSent ? "Request sent" : "Not connected";
        }
        var friendIds = cards.Where(c => c.IsFriend).Select(c => c.UserId).ToList();
        var workouts = db.WorkoutSessions.AsNoTracking().Where(s => friendIds.Contains(s.UserId)).OrderByDescending(s => s.SessionDate).ToList();
        var nutrition = db.DailyNutritions.AsNoTracking().Where(n => friendIds.Contains(n.UserId) && n.Date >= DateTime.UtcNow.Date.AddDays(-9)).ToList();
        var records = db.PersonalRecords.AsNoTracking().Include(r => r.Exercise).Where(r => friendIds.Contains(r.UserId)).ToList();
        var awards = db.UserAchievements.AsNoTracking().Include(a => a.Achievement).Where(a => friendIds.Contains(a.UserId)).ToList();
        foreach (var card in cards.Where(c => c.IsFriend))
        {
            card.LastFiveWorkouts = workouts.Where(s => s.UserId == card.UserId).Take(5).Select(s => new FriendWorkoutPreviewViewModel
            {
                WorkoutSessionId = s.Id,
                Name = s.Name,
                SessionDate = s.SessionDate,
                DurationMinutes = s.DurationMinutes
            }).ToList();
            card.LastFiveWorkoutsCount = card.LastFiveWorkouts.Count;
            card.LastNutritionLogs = nutrition.Where(n => n.UserId == card.UserId).OrderByDescending(n => n.Date).Select(n => new FriendNutritionPreviewViewModel
            {
                Date = n.Date,
                CaloriesKcal = n.CaloriesKcal,
                ProteinG = n.ProteinG,
                FatG = n.FatG,
                CarbsG = n.CarbsG
            }).ToList();
            card.AverageKcal = card.LastNutritionLogs.Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average();
            card.PersonalRecords = records.Where(r => r.UserId == card.UserId).OrderByDescending(r => r.MaxWeightKg).Select(r => new FriendRecordPreviewViewModel
            {
                ExerciseName = r.Exercise.Name,
                MaxWeightKg = r.MaxWeightKg,
                UpdatedAt = r.UpdatedAt
            }).ToList();
            card.Achievements = awards.Where(a => a.UserId == card.UserId).OrderByDescending(a => a.AchievedAt).Select(a => new FriendAchievementPreviewViewModel
            {
                Name = a.Achievement.Name,
                ConditionDescription = a.Achievement.ConditionDescription,
                AchievedAt = a.AchievedAt
            }).ToList();
        }
        return new FriendsViewModel { SearchUsername = search, Friends = cards };
    }

    public void SendRequest(int userId, int otherId)
    {
        if (userId == otherId || !db.Users.Any(u => u.Id == otherId))
            throw new ValidationException("Choose another registered user.");
        var first = Math.Min(userId, otherId);
        var second = Math.Max(userId, otherId);
        if (db.Friendships.Any(f => f.User1Id == first && f.User2Id == second))
            return;
        db.Friendships.Add(new Friendship { User1Id = first, User2Id = second, RequestedByUserId = userId });
        db.SaveChanges();
    }

    public bool Accept(int userId, int otherId)
    {
        var relationship = Find(userId, otherId);
        if (relationship is null || relationship.Status != "Pending" || relationship.RequestedByUserId == userId)
            return false;
        relationship.Accept();
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
        AchievementEngine.AssignAchievements(db, otherId);
        return true;
    }

    public bool Remove(int userId, int otherId)
    {
        var relationship = Find(userId, otherId);
        if (relationship is null)
            return false;
        db.Friendships.Remove(relationship);
        db.SaveChanges();
        return true;
    }

    private Friendship? Find(int userId, int otherId) => db.Friendships.FirstOrDefault(f =>
        f.User1Id == Math.Min(userId, otherId) && f.User2Id == Math.Max(userId, otherId));
}
