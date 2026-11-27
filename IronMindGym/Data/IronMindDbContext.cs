using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

public class IronMindDbContext(DbContextOptions<IronMindDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<InitialBenchmarks> InitialBenchmarks => Set<InitialBenchmarks>();
    public DbSet<WeightLog> WeightLogs => Set<WeightLog>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<SplitType> SplitTypes => Set<SplitType>();
    public DbSet<SplitDay> SplitDays => Set<SplitDay>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<MuscleSubGroup> MuscleSubGroups => Set<MuscleSubGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<SetType> SetTypes => Set<SetType>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<PerformedExercise> PerformedExercises => Set<PerformedExercise>();
    public DbSet<WorkoutSet> Sets => Set<WorkoutSet>();
    public DbSet<WorkoutDraft> WorkoutDrafts => Set<WorkoutDraft>();
    public DbSet<PersonalRecord> PersonalRecords => Set<PersonalRecord>();
    public DbSet<ExerciseProgress> ExerciseProgress => Set<ExerciseProgress>();
    public DbSet<DailyNutrition> DailyNutritions => Set<DailyNutrition>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<ActivityLike> ActivityLikes => Set<ActivityLike>();
    public DbSet<FriendComment> FriendComments => Set<FriendComment>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<ResearchItem> ResearchItems => Set<ResearchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext keeps the entered spelling while unique indexes and lookups ignore letter case.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<User>(user =>
        {
            // citext has no length of its own, so check constraints hold the limits.
            user.Property(u => u.Email).HasColumnType("citext");
            user.Property(u => u.Username).HasColumnType("citext");
            user.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Users_EmailLength", "char_length(\"Email\") <= 120");
                table.HasCheckConstraint("CK_Users_UsernameLength", "char_length(\"Username\") <= 50");
            });
            user.HasIndex(u => u.Email).IsUnique();
            user.HasIndex(u => u.Username).IsUnique();
            user.HasOne(u => u.Profile).WithOne(p => p.User).HasForeignKey<Profile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            user.HasOne(u => u.Benchmarks).WithOne(b => b.User).HasForeignKey<InitialBenchmarks>(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
            user.HasOne(u => u.Plan).WithOne(p => p.User).HasForeignKey<WorkoutPlan>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        // A user has at most one of each: the user id is the key.
        modelBuilder.Entity<Profile>(profile =>
        {
            profile.HasKey(p => p.UserId);
            profile.Property(p => p.EnergyEquation).HasConversion<string>().HasMaxLength(8);
            profile.Property(p => p.Language).HasMaxLength(8).HasConversion(
                language => language == Language.Ukrainian ? "uk" : "en",
                code => code == "uk" ? Language.Ukrainian : Language.English);
        });
        modelBuilder.Entity<InitialBenchmarks>().HasKey(b => b.UserId);
        modelBuilder.Entity<WorkoutPlan>().HasKey(p => p.UserId);

        // Moments are timestamptz in UTC; calendar days in the athlete's time zone are DateOnly, stored as date.
        modelBuilder.Entity<WeightLog>().HasIndex(w => new { w.UserId, w.Date }).IsUnique();
        modelBuilder.Entity<DailyNutrition>().HasIndex(n => new { n.UserId, n.Date }).IsUnique();

        // Reference rows are found by a stable key: a code where the code relies on the row, otherwise the
        // English name. The seeder matches on the same keys, so every key is unique.
        modelBuilder.Entity<SplitType>(split =>
        {
            split.HasIndex(s => s.Code).IsUnique();
            split.HasIndex(s => s.Name).IsUnique();
        });
        modelBuilder.Entity<SplitDay>(day =>
        {
            day.HasIndex(d => d.Code).IsUnique();
            day.HasIndex(d => new { d.SplitTypeId, d.Position }).IsUnique();
            day.HasOne(d => d.SplitType).WithMany(s => s.Days).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SetType>(setType =>
        {
            setType.HasIndex(s => s.Code).IsUnique();
            setType.HasIndex(s => s.Name).IsUnique();
        });
        modelBuilder.Entity<Exercise>(exercise =>
        {
            exercise.HasIndex(e => e.Name).IsUnique();
            exercise.Property(e => e.KeyLift).HasConversion<string>().HasMaxLength(20);
            exercise.HasIndex(e => e.KeyLift).IsUnique();
        });
        modelBuilder.Entity<MuscleGroup>().HasIndex(g => g.Name).IsUnique();
        modelBuilder.Entity<MuscleSubGroup>().HasIndex(s => new { s.MuscleGroupId, s.Name }).IsUnique();
        modelBuilder.Entity<Avatar>().HasIndex(a => a.Name).IsUnique();
        modelBuilder.Entity<ResearchItem>().HasIndex(r => r.Title).IsUnique();
        modelBuilder.Entity<Achievement>().HasIndex(a => a.Code).IsUnique();

        // Reference data is shared by every athlete. Deleting a row that is still in use fails
        // instead of taking the workouts, records and awards that point at it along.
        modelBuilder.Entity<MuscleSubGroup>().HasOne(s => s.MuscleGroup).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Exercise>().HasOne(e => e.MuscleSubGroup).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PerformedExercise>().HasOne(p => p.Exercise).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PersonalRecord>().HasOne(r => r.Exercise).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkoutSet>().HasOne(s => s.SetType).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkoutPlan>().HasOne(p => p.SplitType).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkoutSession>().HasOne(s => s.SplitDay).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UserAchievement>().HasOne(a => a.Achievement).WithMany().OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutSession>(session =>
        {
            session.HasIndex(s => new { s.UserId, s.SessionDate });
            session.HasIndex(s => new { s.UserId, s.DraftId }).IsUnique();
        });
        modelBuilder.Entity<WorkoutDraft>(draft =>
        {
            draft.HasKey(d => d.UserId);
            draft.Property(d => d.Content).HasColumnType("jsonb");
            draft.HasOne(d => d.User).WithOne().HasForeignKey<WorkoutDraft>(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalRecord>().HasIndex(r => new { r.UserId, r.ExerciseId }).IsUnique();
        // Exercise history and statistics look up the workouts of an exercise.
        modelBuilder.Entity<PerformedExercise>().HasIndex(p => new { p.ExerciseId, p.WorkoutSessionId });
        modelBuilder.Entity<ExerciseProgress>(progress =>
        {
            progress.HasIndex(p => new { p.UserId, p.ExerciseId }).IsUnique();
            progress.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            progress.HasOne(p => p.Exercise).WithMany().HasForeignKey(p => p.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<UserAchievement>().HasIndex(a => new { a.UserId, a.AchievementId }).IsUnique();

        // Invariants every row keeps, whoever writes it. Request validation, with tighter limits and
        // messages for the athlete, stays in the contracts and services.
        Checks<Profile>(modelBuilder, "Profiles",
            ("Age", "\"Age\" >= 0"),
            ("HeightCm", "\"HeightCm\" > 0"),
            ("EnergyEquation", "\"EnergyEquation\" IN ('None', 'Male', 'Female')"),
            ("Language", "\"Language\" IN ('en', 'uk')"),
            ("Targets", "\"CalorieTarget\" > 0 AND \"ProteinTarget\" > 0 AND \"FatTarget\" > 0 AND \"CarbsTarget\" > 0"));
        Checks<InitialBenchmarks>(modelBuilder, "InitialBenchmarks",
            ("Loads", "\"BenchPressKg\" >= 0 AND \"SquatKg\" >= 0 AND \"DeadliftKg\" >= 0"));
        Checks<WeightLog>(modelBuilder, "WeightLogs", ("ValueKg", "\"ValueKg\" > 0"));
        Checks<WorkoutPlan>(modelBuilder, "WorkoutPlans", ("WorkoutsPerWeek", "\"WorkoutsPerWeek\" BETWEEN 1 AND 7"));
        Checks<WorkoutSession>(modelBuilder, "WorkoutSessions",
            ("DurationMinutes", "\"DurationMinutes\" >= 0"),
            ("Rating", "\"Rating\" BETWEEN 1 AND 5"));
        Checks<WorkoutSet>(modelBuilder, "Sets",
            ("SetNumber", "\"SetNumber\" >= 1"),
            ("Values", "\"WeightKg\" >= 0 AND \"Reps\" >= 0 AND \"DurationSeconds\" >= 0 AND \"RestSeconds\" >= 0"),
            ("Intensity", "\"Intensity\" BETWEEN 0 AND 10"));
        Checks<PerformedExercise>(modelBuilder, "PerformedExercises", ("OrderInWorkout", "\"OrderInWorkout\" >= 1"));
        Checks<Exercise>(modelBuilder, "Exercises", ("KeyLift", "\"KeyLift\" IN ('BenchPress', 'Squat', 'Deadlift')"));
        Checks<PersonalRecord>(modelBuilder, "PersonalRecords", ("MaxWeightKg", "\"MaxWeightKg\" > 0"));
        Checks<ExerciseProgress>(modelBuilder, "ExerciseProgress",
            ("Values", "\"InitialWeightKg\" >= 0 AND \"InitialReps\" >= 0 AND \"InitialDurationSeconds\" >= 0 AND " +
                "\"CurrentWeightKg\" >= 0 AND \"CurrentReps\" >= 0 AND \"CurrentDurationSeconds\" >= 0"),
            ("Intensity", "\"InitialIntensity\" BETWEEN 0 AND 10 AND \"CurrentIntensity\" BETWEEN 0 AND 10"));
        Checks<DailyNutrition>(modelBuilder, "DailyNutritions",
            ("Intake", "\"CaloriesKcal\" >= 0 AND \"ProteinG\" >= 0 AND \"FatG\" >= 0 AND \"CarbsG\" >= 0"));
        Checks<Avatar>(modelBuilder, "Avatars", ("MinStrengthCoefficient", "\"MinStrengthCoefficient\" >= 0"));
        Checks<ResearchItem>(modelBuilder, "ResearchItems", ("SourceUrl", "\"SourceUrl\" LIKE 'https://%'"));

        modelBuilder.Entity<Friendship>(friendship =>
        {
            friendship.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Friendships_OrderedPair", "\"User1Id\" < \"User2Id\"");
                table.HasCheckConstraint("CK_Friendships_RequesterInPair", "\"RequestedByUserId\" IN (\"User1Id\", \"User2Id\")");
                table.HasCheckConstraint("CK_Friendships_Status", "\"Status\" IN ('Pending', 'Accepted')");
            });
            friendship.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);
            friendship.HasIndex(f => new { f.User1Id, f.User2Id }).IsUnique();
            friendship.HasOne(f => f.User1).WithMany().HasForeignKey(f => f.User1Id).OnDelete(DeleteBehavior.Cascade);
            friendship.HasOne(f => f.User2).WithMany().HasForeignKey(f => f.User2Id).OnDelete(DeleteBehavior.Cascade);
            friendship.HasOne<User>().WithMany().HasForeignKey(f => f.RequestedByUserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ActivityLike>(like =>
        {
            like.HasIndex(l => new { l.UserId, l.WorkoutSessionId }).IsUnique();
            like.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
            like.HasOne(l => l.WorkoutSession).WithMany().HasForeignKey(l => l.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FriendComment>(comment =>
        {
            comment.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Cascade);
            comment.HasOne(c => c.WorkoutSession).WithMany().HasForeignKey(c => c.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void Checks<TEntity>(ModelBuilder modelBuilder, string table, params (string Name, string Sql)[] checks)
        where TEntity : class =>
        modelBuilder.Entity<TEntity>().ToTable(builder =>
        {
            foreach (var (name, sql) in checks)
                builder.HasCheckConstraint($"CK_{table}_{name}", sql);
        });
}
