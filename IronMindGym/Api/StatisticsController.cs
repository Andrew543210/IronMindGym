using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class StatisticsController(StatisticsService statistics, AchievementService achievements) : ApiController
{
    [HttpGet("api/statistics")]
    public Task<TrainingStatistics> Summary(CancellationToken cancellationToken) => statistics.BuildAsync(UserId, cancellationToken);

    [HttpGet("api/statistics/records")]
    public Task<List<PersonalRecordItem>> Records(CancellationToken cancellationToken) => statistics.RecordsAsync(UserId, cancellationToken);

    [HttpGet("api/awards")]
    public Task<List<AwardProgressItem>> Awards(CancellationToken cancellationToken) => achievements.ProgressAsync(UserId, cancellationToken);
}
