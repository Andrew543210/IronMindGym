using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class StatisticsController(StatisticsService statistics, AchievementService achievements) : ApiController
{
    [HttpGet("api/statistics")]
    public TrainingStatistics Summary() => statistics.Build(UserId);

    [HttpGet("api/statistics/records")]
    public List<PersonalRecordItem> Records() => statistics.Records(UserId);

    [HttpGet("api/awards")]
    public List<AwardProgressItem> Awards() => achievements.Progress(UserId);
}
