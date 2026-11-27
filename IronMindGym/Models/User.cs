using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; }

    public Profile? Profile { get; set; }
    public InitialBenchmarks? Benchmarks { get; set; }
    public WorkoutPlan? Plan { get; set; }
    public ICollection<WeightLog> WeightLogs { get; set; } = [];
}
