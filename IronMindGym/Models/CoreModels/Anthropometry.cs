using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Anthropometry
{
    public int Id { get; set; }

    [Range(0, 500)]
    public double WeightKg { get; set; }

    [Range(0, 300)]
    public double HeightCm { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Validation Methods
    public bool IsValidWeight(double weight) => weight > 0 && weight <= 500;
    public bool IsValidHeight(double height) => height > 0 && height <= 300;

    public void ValidateAndSet(double weight, double height)
    {
        if (!IsValidWeight(weight))
            throw new ArgumentException("Weight must be between 0.1 and 500 kg.");
        if (!IsValidHeight(height))
            throw new ArgumentException("Height must be between 0.1 and 300 cm.");

        WeightKg = weight;
        HeightCm = height;
        RecordedAt = DateTime.UtcNow;
    }

    public string FormatDisplay() => $"{WeightKg} kg / {HeightCm} cm";

    public override string ToString() => $"Anthropometry: {FormatDisplay()}";
}
