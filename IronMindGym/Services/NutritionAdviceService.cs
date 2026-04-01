using IronMindGym.Models;

namespace IronMindGym.Services;

public class NutritionAdviceService : INutritionAdviceService
{
    public string BuildAdvice(DailyNutrition nutrition)
    {
        if (nutrition.ProteinG < 120)
        {
            return "Порада: додай більше білка (м'ясо, риба, яйця або протеїн).";
        }

        if (nutrition.CaloriesKcal < 2000)
        {
            return "Порада: збільш калораж для стабільного прогресу в силі.";
        }

        if (nutrition.FatG < 50)
        {
            return "Порада: додай корисні жири (горіхи, оливкова олія, авокадо).";
        }

        return "Харчування виглядає збалансовано. Продовжуй у тому ж темпі.";
    }
}

