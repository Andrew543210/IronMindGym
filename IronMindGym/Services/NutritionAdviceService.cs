using IronMindGym.Models;

namespace IronMindGym.Services;

public class NutritionAdviceService : INutritionAdviceService
{
    public NutritionAdviceResult BuildAdvice(
        DailyNutrition nutrition,
        NutritionProfileSnapshot profile,
        IReadOnlyList<DailyNutrition> recentLogs)
    {
        var weightKg = Math.Max(profile.WeightKg ?? 72, 35);
        var heightCm = Math.Max(profile.HeightCm ?? 175, 130);
        var age = Math.Clamp(profile.Age ?? 25, 12, 90);
        var pal = ResolvePal(profile.WorkoutsPerWeek);
        var bmr = CalculateMifflinStJeor(weightKg, heightCm, age, profile.Gender);
        var genderOffset = profile.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) ? -161 : 5;

        var targetCalories = Math.Round(bmr * pal, 0);
        var targetProtein = Math.Round(weightKg * 1.9, 0);
        var targetFat = Math.Round(weightKg * 1.0, 0);
        var targetCarbs = Math.Round(weightKg * 4.0, 0);

        var minProtein = weightKg * 1.5;
        var maxProtein = weightKg * 2.2;
        var minFat = weightKg * 0.8;
        var maxFat = weightKg * 1.2;
        var minCarbs = weightKg * 3.0;
        var maxCarbs = weightKg * 5.0;

        var minCalories = targetCalories * 0.92;
        var maxCalories = targetCalories * 1.08;

        var tips = new List<string>();
        var score = 100;

        void Penalize(int points) => score = Math.Max(score - points, 0);

        if (nutrition.ProteinG < minProtein)
        {
            Penalize(20);
            tips.Add($"Protein is below your athlete range ({Math.Round(minProtein)}-{Math.Round(maxProtein)} g/day). Add lean meat, eggs, Greek yogurt, cottage cheese, or whey around workouts.");
        }
        else if (nutrition.ProteinG > maxProtein)
        {
            Penalize(6);
            tips.Add("Protein is above the useful range for strength progress. Keep protein near target and move excess calories to carbohydrates for better training output.");
        }

        if (nutrition.FatG < minFat)
        {
            Penalize(15);
            tips.Add($"Fat intake is low (target range: {Math.Round(minFat)}-{Math.Round(maxFat)} g). Add olive oil, nuts, avocado, or fatty fish to support hormones and recovery.");
        }
        else if (nutrition.FatG > maxFat)
        {
            Penalize(6);
            tips.Add("Fat is above the recommended range. Reduce hidden fats in snacks/sauces and keep most calories in carbs for high-quality lifting sessions.");
        }

        if (nutrition.CarbsG < minCarbs)
        {
            Penalize(12);
            tips.Add($"Carbohydrates are below performance range ({Math.Round(minCarbs)}-{Math.Round(maxCarbs)} g). Add rice, oats, potatoes, fruits, and pre/post-workout carbs for power output.");
        }
        else if (nutrition.CarbsG > maxCarbs)
        {
            Penalize(6);
            tips.Add("Carbohydrates are above the standard range. Keep portions steady and monitor weekly body-weight trend to avoid excessive fat gain.");
        }

        if (nutrition.CaloriesKcal < minCalories)
        {
            Penalize(20);
            tips.Add($"Calories are below your calculated maintenance (~{targetCalories} kcal by Mifflin-St Jeor x PAL {pal:F2}). Increase by 200-300 kcal to support progression.");
        }
        else if (nutrition.CaloriesKcal > maxCalories)
        {
            Penalize(10);
            tips.Add($"Calories are above target (~{targetCalories} kcal). If weekly weight gain is too fast, reduce daily intake by 150-250 kcal.");
        }

        var weekLogs = recentLogs
            .Where(x => x.Date >= DateTime.UtcNow.Date.AddDays(-6))
            .ToList();

        if (weekLogs.Count >= 3)
        {
            var avgWeekCalories = weekLogs.Average(x => x.CaloriesKcal);
            var drift = Math.Abs(nutrition.CaloriesKcal - avgWeekCalories);
            if (drift > 350)
            {
                Penalize(8);
                tips.Add("Today differs strongly from your 7-day calorie average. Keep calories and macro timing consistent for more predictable strength and body-composition progress.");
            }
        }

        var proteinCalories = nutrition.ProteinG * 4;
        var fatCalories = nutrition.FatG * 9;
        var carbsCalories = nutrition.CarbsG * 4;
        var trackedCalories = proteinCalories + fatCalories + carbsCalories;

        if (Math.Abs(trackedCalories - nutrition.CaloriesKcal) > 150)
        {
            Penalize(5);
            tips.Add("Your logged calories do not match macro-derived calories. Recheck food labels and serving sizes for cleaner tracking.");
        }

        if (tips.Count == 0)
        {
            tips.Add("Great balance across calories, protein, fats, and carbs. Keep the same structure and review body-weight/strength trend every 7 days.");
        }

        tips.Insert(0,
            $"Formula baseline: BMR {Math.Round(bmr)} kcal (Mifflin-St Jeor), activity multiplier {pal:F2}, maintenance target ~{targetCalories} kcal. Macro targets: protein {targetProtein} g, fat {targetFat} g, carbs {targetCarbs} g.");

        var verdict = score switch
        {
            >= 85 => "Excellent",
            >= 70 => "Good",
            >= 50 => "Needs improvement",
            _ => "Critical"
        };

        return new NutritionAdviceResult
        {
            Verdict = verdict,
            Summary = $"Nutrition evaluation: {verdict} ({score}/100)",
            Score = score,
            Tips = tips.Take(6).ToList(),
            TargetCaloriesKcal = targetCalories,
            TargetProteinG = targetProtein,
            TargetFatG = targetFat,
            TargetCarbsG = targetCarbs,
            BasalMetabolicRate = Math.Round(bmr, 0),
            ActivityMultiplier = pal,
            BmrFormulaText = $"BMR = 10*{weightKg:F1} + 6.25*{heightCm:F1} - 5*{age} {(genderOffset >= 0 ? "+" : "-")} {Math.Abs(genderOffset)} = {Math.Round(bmr)} kcal",
            CaloriesFormulaText = $"Calories target = BMR ({Math.Round(bmr)}) * PAL ({pal:F2}) = {targetCalories} kcal",
            ProteinFormulaText = $"Protein target = 1.9 * weight({weightKg:F1}) = {targetProtein} g/day",
            FatFormulaText = $"Fat target = 1.0 * weight({weightKg:F1}) = {targetFat} g/day",
            CarbsFormulaText = $"Carbs target = 4.0 * weight({weightKg:F1}) = {targetCarbs} g/day"
        };
    }

    private static double ResolvePal(int workoutsPerWeek)
    {
        return workoutsPerWeek switch
        {
            <= 1 => 1.3,
            2 => 1.4,
            3 => 1.5,
            4 => 1.6,
            5 => 1.7,
            _ => 1.8
        };
    }

    private static double CalculateMifflinStJeor(double weightKg, double heightCm, int age, string gender)
    {
        var isFemale = gender.Equals("Female", StringComparison.OrdinalIgnoreCase);
        var baseValue = 10 * weightKg + 6.25 * heightCm - 5 * age;
        return isFemale ? baseValue - 161 : baseValue + 5;
    }
}

