using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IronMindGym.Data;

public class IronMindDbContext(DbContextOptions<IronMindDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Anthropometry> AnthropometryLogs => Set<Anthropometry>();
    public DbSet<InitialBenchmarks> InitialBenchmarks => Set<InitialBenchmarks>();
    public DbSet<WeightLog> WeightLogs => Set<WeightLog>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<SplitType> SplitTypes => Set<SplitType>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<MuscleSubGroup> MuscleSubGroups => Set<MuscleSubGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<SetType> SetTypes => Set<SetType>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<PerformedExercise> PerformedExercises => Set<PerformedExercise>();
    public DbSet<WorkoutSet> Sets => Set<WorkoutSet>();
    public DbSet<WorkoutReview> WorkoutReviews => Set<WorkoutReview>();
    public DbSet<PersonalRecord> PersonalRecords => Set<PersonalRecord>();
    public DbSet<ExerciseProgress> ExerciseProgress => Set<ExerciseProgress>();
    public DbSet<DailyNutrition> DailyNutritions => Set<DailyNutrition>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<ActivityLike> ActivityLikes => Set<ActivityLike>();
    public DbSet<FriendComment> FriendComments => Set<FriendComment>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<ResearchItem> ResearchItems => Set<ResearchItem>();
    public DbSet<ActivityFeed> ActivityFeeds => Set<ActivityFeed>();

    /// <summary>
    /// Every moment is stored as UTC. PostgreSQL timestamptz rejects values of unknown kind,
    /// so values without a kind, such as calendar days, are marked as UTC.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext keeps the entered spelling while unique indexes and lookups ignore letter case.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasColumnType("citext");
            user.Property(u => u.Username).HasColumnType("citext");
            user.HasIndex(u => u.Email).IsUnique();
            user.HasIndex(u => u.Username).IsUnique();
            user.HasOne(u => u.Profile).WithOne(p => p.User).HasForeignKey<Profile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            user.HasOne(u => u.Benchmarks).WithOne(b => b.User).HasForeignKey<InitialBenchmarks>(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeightLog>(log =>
        {
            log.Property(w => w.Date).HasColumnType("date");
            log.HasIndex(w => new { w.UserId, w.Date }).IsUnique();
        });
        modelBuilder.Entity<DailyNutrition>(log =>
        {
            log.Property(n => n.Date).HasColumnType("date");
            log.HasIndex(n => new { n.UserId, n.Date }).IsUnique();
        });
        modelBuilder.Entity<UserGoal>().Property(g => g.Deadline).HasColumnType("date");

        modelBuilder.Entity<Exercise>().HasIndex(e => e.Name).IsUnique();
        modelBuilder.Entity<Achievement>().HasIndex(a => a.Code).IsUnique();

        modelBuilder.Entity<WorkoutSession>(session =>
        {
            session.HasIndex(s => new { s.UserId, s.SessionDate });
            session.HasIndex(s => new { s.UserId, s.DraftId }).IsUnique();
            session.HasOne(s => s.Review).WithOne(r => r.WorkoutSession).HasForeignKey<WorkoutReview>(r => r.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalRecord>().HasIndex(r => new { r.UserId, r.ExerciseId }).IsUnique();
        modelBuilder.Entity<ExerciseProgress>(progress =>
        {
            progress.HasIndex(p => new { p.UserId, p.ExerciseId }).IsUnique();
            progress.HasOne(p => p.User).WithMany(u => u.ExerciseProgress).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            progress.HasOne(p => p.Exercise).WithMany().HasForeignKey(p => p.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserAchievement>().HasIndex(a => new { a.UserId, a.AchievementId }).IsUnique();

        modelBuilder.Entity<Friendship>(friendship =>
        {
            friendship.HasIndex(f => new { f.User1Id, f.User2Id }).IsUnique();
            friendship.HasOne(f => f.User1).WithMany(u => u.FriendshipsAsUser1).HasForeignKey(f => f.User1Id).OnDelete(DeleteBehavior.Restrict);
            friendship.HasOne(f => f.User2).WithMany(u => u.FriendshipsAsUser2).HasForeignKey(f => f.User2Id).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ActivityLike>(like =>
        {
            like.HasIndex(l => new { l.UserId, l.WorkoutSessionId }).IsUnique();
            like.HasOne(l => l.User).WithMany(u => u.ActivityLikes).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
            like.HasOne(l => l.WorkoutSession).WithMany(s => s.Likes).HasForeignKey(l => l.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FriendComment>(comment =>
        {
            comment.HasOne(c => c.Author).WithMany(u => u.FriendComments).HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Cascade);
            comment.HasOne(c => c.WorkoutSession).WithMany(s => s.Comments).HasForeignKey(c => c.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
