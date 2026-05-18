using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

public class IronMindDbContext(DbContextOptions<IronMindDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Anthropometry> AnthropometryLogs => Set<Anthropometry>();
    public DbSet<InitialBenchmarks> InitialBenchmarks => Set<InitialBenchmarks>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<SplitType> SplitTypes => Set<SplitType>();
    public DbSet<WeekDay> WeekDays => Set<WeekDay>();
    public DbSet<ScheduledDay> ScheduledDays => Set<ScheduledDay>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<MuscleSubGroup> MuscleSubGroups => Set<MuscleSubGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<PerformedExercise> PerformedExercises => Set<PerformedExercise>();
    public DbSet<WorkoutSet> Sets => Set<WorkoutSet>();
    public DbSet<SetType> SetTypes => Set<SetType>();
    public DbSet<WorkoutReview> WorkoutReviews => Set<WorkoutReview>();
    public DbSet<DailyNutrition> DailyNutritions => Set<DailyNutrition>();
    public DbSet<NutritionAdvice> NutritionAdvices => Set<NutritionAdvice>();
    public DbSet<NutritionAssessment> NutritionAssessments => Set<NutritionAssessment>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<ResearchItem> ResearchItems => Set<ResearchItem>();
    public DbSet<ActivityFeed> ActivityFeeds => Set<ActivityFeed>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<PersonalRecord> PersonalRecords => Set<PersonalRecord>();
    public DbSet<WeightLog> WeightLogs => Set<WeightLog>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UITheme> UIThemes => Set<UITheme>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<ActivityLike> ActivityLikes => Set<ActivityLike>();
    public DbSet<FriendComment> FriendComments => Set<FriendComment>();
    public DbSet<ExerciseProgress> ExerciseProgress => Set<ExerciseProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Achievement>()
            .HasIndex(a => a.Code)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Benchmarks)
            .WithOne(b => b.User)
            .HasForeignKey<InitialBenchmarks>(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(ws => ws.Review)
            .WithOne(r => r.WorkoutSession)
            .HasForeignKey<WorkoutReview>(r => r.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User1)
            .WithMany(u => u.FriendshipsAsUser1)
            .HasForeignKey(f => f.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User2)
            .WithMany(u => u.FriendshipsAsUser2)
            .HasForeignKey(f => f.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NutritionAssessment>()
            .HasOne(na => na.DailyNutrition)
            .WithOne(dn => dn.Assessment)
            .HasForeignKey<NutritionAssessment>(na => na.DailyNutritionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityLike>()
            .HasIndex(x => new { x.UserId, x.WorkoutSessionId })
            .IsUnique();

        modelBuilder.Entity<ActivityLike>()
            .HasOne(x => x.User)
            .WithMany(u => u.ActivityLikes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityLike>()
            .HasOne(x => x.WorkoutSession)
            .WithMany(ws => ws.Likes)
            .HasForeignKey(x => x.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FriendComment>()
            .HasOne(x => x.Author)
            .WithMany(u => u.FriendComments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FriendComment>()
            .HasOne(x => x.WorkoutSession)
            .WithMany(ws => ws.Comments)
            .HasForeignKey(x => x.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseProgress>()
            .HasOne(ep => ep.User)
            .WithMany(u => u.ExerciseProgress)
            .HasForeignKey(ep => ep.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseProgress>()
            .HasOne(ep => ep.Exercise)
            .WithMany()
            .HasForeignKey(ep => ep.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseProgress>()
            .HasIndex(ep => new { ep.UserId, ep.ExerciseId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}

