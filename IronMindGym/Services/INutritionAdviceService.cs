using IronMindGym.Models;

namespace IronMindGym.Services;

public interface INutritionAdviceService
{
    string BuildAdvice(DailyNutrition nutrition);
}

