using IronMindGym.Models;

namespace IronMindGym.Services;

public class NutritionAdviceService : INutritionAdviceService
{
    public NutritionAdviceResult BuildAdvice(DailyNutrition nutrition, double? bodyWeightKg, IReadOnlyList<DailyNutrition> recentLogs)
    {
        var tips = new List<string>();
        var score = 100;

        var targetProtein = Math.Round((bodyWeightKg ?? 72) * 1.8, 0);
        var minFat = Math.Round((bodyWeightKg ?? 72) * 0.8, 0);
        var minCarbs = Math.Round((bodyWeightKg ?? 72) * 2.5, 0);
        var targetCalories = Math.Round((bodyWeightKg ?? 72) * 33, 0);

        var minCalories = targetCalories * 0.9;
        var maxCalories = targetCalories * 1.1;

        void Penalize(int points) => score = Math.Max(score - points, 0);

        if (nutrition.ProteinG < targetProtein * 0.9)
        {
            Penalize(20);
            tips.Add($"Підніми білок до ~{targetProtein} г/день (зараз {nutrition.ProteinG} г). Це прискорить відновлення.");
        }
        else if (nutrition.ProteinG > targetProtein * 1.45)
        {
            Penalize(5);
            tips.Add("Білка забагато відносно цілі. Можна частину калорій повернути у вуглеводи.");
        }

        if (nutrition.FatG < minFat)
        {
            Penalize(15);
            tips.Add($"Жири нижче норми (~{minFat} г). Додай яйця, горіхи, оливкову олію або жирну рибу.");
        }

        if (nutrition.CarbsG < minCarbs)
        {
            Penalize(10);
            tips.Add($"Вуглеводи низькі для силових (~{minCarbs} г+). Додай крупи, картоплю або фрукти до/після тренування.");
        }

        if (nutrition.CaloriesKcal < minCalories)
        {
            Penalize(20);
            tips.Add($"Калорій замало для прогресу (ціль ~{targetCalories} ккал). Додай 200-300 ккал стабільно щодня.");
        }
        else if (nutrition.CaloriesKcal > maxCalories)
        {
            Penalize(10);
            tips.Add($"Калорії вище цілі (~{targetCalories} ккал). Зменш на 150-250 ккал, якщо набір ваги занадто швидкий.");
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
                tips.Add("Сьогоднішній калораж сильно відхиляється від твого середнього за тиждень. Стабільність = кращий прогноз прогресу.");
            }
        }

        if (tips.Count == 0)
        {
            tips.Add("Харчування добре збалансоване. Тримай поточний курс і контролюй вагу щотижня.");
        }

        var verdict = score switch
        {
            >= 85 => "Відмінно",
            >= 70 => "Добре",
            >= 50 => "Потрібне покращення",
            _ => "Критично"
        };

        return new NutritionAdviceResult
        {
            Verdict = verdict,
            Summary = $"Оцінка харчування: {verdict} ({score}/100)",
            Score = score,
            Tips = tips.Take(4).ToList()
        };
    }
}

