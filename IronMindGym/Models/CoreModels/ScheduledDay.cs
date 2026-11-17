namespace IronMindGym.Models;

public class ScheduledDay
{
    public int Id
    {
        get; set;
    }
    public int WorkoutPlanId
    {
        get; set;
    }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public int WeekDayId
    {
        get; set;
    }
    public WeekDay WeekDay { get; set; } = null!;

    public void ReassignWeekDay(int weekDayId) => WeekDayId = weekDayId;

    public override string ToString() => $"ScheduledDay: Plan={WorkoutPlanId}, WeekDay={WeekDayId}";
}
