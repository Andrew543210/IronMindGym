using IronMindGym.Contracts;
using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class StatisticsController(IronMindDbContext db, StatisticsService statistics) : ApiController
{
    [HttpGet("api/statistics")]
    public TrainingStatistics Summary() => statistics.Build(UserId);

    [HttpGet("api/statistics/records")]
    public List<PersonalRecordItem> Records() => statistics.Records(UserId);

    [HttpGet("api/awards")]
    public List<AwardProgressItem> Awards() => AchievementEngine.BuildAwardsProgress(db, UserId);
}
