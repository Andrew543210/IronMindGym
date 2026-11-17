using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class DashboardService(IronMindDbContext db, IAvatarService avatarService, StrengthService strengthService)
{
    public DashboardViewModel Build(int currentUserId)
    {


        var user = db.Users
            .Include(u => u.Benchmarks)
            .Single(u => u.Id == currentUserId);

        var latestAnthropometry = db.AnthropometryLogs
            .Where(a => a.UserId == currentUserId)
            .OrderByDescending(a => a.RecordedAt)
            .FirstOrDefault();

        var activePlan = db.WorkoutPlans
            .FirstOrDefault(p => p.UserId == currentUserId && p.IsActive);

        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.Date.DayOfWeek + (int)DayOfWeek.Monday);
        if (startOfWeek > DateTime.UtcNow.Date)
        {
            startOfWeek = startOfWeek.AddDays(-7);
        }

        var completedThisWeek = db.WorkoutSessions.Count(s => s.UserId == currentUserId && s.SessionDate >= startOfWeek);

        var feed = db.ActivityFeeds
            .Include(f => f.ResearchItem)
            .OrderByDescending(f => f.PublishedAt)
            .Take(4)
            .ToList();

        var currentStrength = strengthService.Current(currentUserId, user.Benchmarks);
        var strengthCoefficient = avatarService.CalculateStrengthCoefficient(currentStrength, latestAnthropometry);
        var avatars = db.Avatars.AsNoTracking().OrderBy(a => a.MinStrengthCoefficient).ToList();
        var avatar = avatarService.ResolveAvatar(avatars, currentStrength, latestAnthropometry);
        var avatarScalePoints = BuildAvatarScalePoints(avatars);
        foreach (var point in avatarScalePoints)
        {
            point.IsCurrent = point.Name.Equals(avatar.Name, StringComparison.OrdinalIgnoreCase);
        }

        var vm = new DashboardViewModel
        {
            NeedsSetup = user.Benchmarks == null || latestAnthropometry == null,
            WeightKg = latestAnthropometry?.WeightKg,
            TotalWorkouts = db.WorkoutSessions.Count(w => w.UserId == currentUserId),
            TodayNutrition = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == currentUserId && n.Date == DateTime.UtcNow.Date),
            Goals = db.UserGoals.AsNoTracking().Where(g => g.UserId == currentUserId && !g.IsCompleted).OrderBy(g => g.Deadline).Take(3).ToList(),
            RecentWorkouts = db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == currentUserId).OrderByDescending(w => w.SessionDate).Take(3).ToList(),
            Username = user.Username,
            AvatarName = avatar.Name,
            StrengthCoefficient = strengthCoefficient,
            CurrentAvatarPositionPercent = CalculateEqualScalePosition(strengthCoefficient, avatars),
            PlannedWorkoutsPerWeek = activePlan?.WorkoutsPerWeek ?? 0,
            CompletedThisWeek = completedThisWeek,
            AvatarScalePoints = avatarScalePoints,
            FeedItems = feed
                .Select(f => new DashboardResearchItemViewModel
                {
                    FeedId = f.Id,
                    PublishedAt = f.PublishedAt,
                    Title = f.ResearchItem.Title,
                    ThesisText = f.ResearchItem.ThesisText,
                    Source = BuildSourceName(f.ResearchItem.Source),
                    SourceUrl = BuildSourceUrl(f.ResearchItem.Source)
                })
                .ToList()
        };

        return vm;
    }


    public static string BuildSourceName(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "Research Source";
        }

        var parts = source.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? source : parts[0];
    }

    public static string BuildSourceUrl(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var parts = source.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return Uri.TryCreate(parts[1], UriKind.Absolute, out var link) && link.Scheme == "https" ? link.AbsoluteUri : string.Empty;
        }

        return Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.AbsoluteUri : string.Empty;
    }

    private static List<AvatarScalePointViewModel> BuildAvatarScalePoints(List<Avatar> avatars)
    {
        if (avatars.Count == 0)
        {
            return new List<AvatarScalePointViewModel>();
        }

        var maxIndex = avatars.Count - 1;
        return avatars
            .Select((a, index) => new AvatarScalePointViewModel
            {
                Name = a.Name,
                MinStrengthCoefficient = a.MinStrengthCoefficient,
                PositionPercent = maxIndex == 0 ? 0 : index * 100.0 / maxIndex,
                RangeLabel = BuildRangeLabel(index, avatars),
                IsCurrent = false
            })
            .ToList();
    }

    private static string BuildRangeLabel(int index, List<Avatar> avatars)
    {
        var start = avatars[index].MinStrengthCoefficient;
        if (index == avatars.Count - 1)
        {
            return $"{start:0.0} - 5.0";
        }

        var nextStart = avatars[index + 1].MinStrengthCoefficient;
        var end = Math.Round(nextStart - 0.1, 1);
        return $"{start:0.0} - {end:0.0}";
    }

    private static double CalculateEqualScalePosition(double coefficient, List<Avatar> avatars)
    {
        if (avatars.Count <= 1)
        {
            return 0;
        }

        var sorted = avatars.OrderBy(a => a.MinStrengthCoefficient).ToList();
        if (coefficient <= sorted[0].MinStrengthCoefficient)
        {
            return 0;
        }

        var maxIndex = sorted.Count - 1;
        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var left = sorted[i].MinStrengthCoefficient;
            var right = sorted[i + 1].MinStrengthCoefficient;
            if (coefficient <= right)
            {
                var segmentPercent = (double)i / maxIndex;
                var segmentSize = 1.0 / maxIndex;
                var ratio = right <= left ? 0 : (coefficient - left) / (right - left);
                return Math.Clamp((segmentPercent + ratio * segmentSize) * 100.0, 0, 100);
            }
        }

        return 100;
    }
}
