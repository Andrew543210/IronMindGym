using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Services;

public class GoalService(IronMindDbContext db)
{
    public List<UserGoal> List(int userId) => db.UserGoals.Where(g => g.UserId == userId)
        .OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline).ToList();

    public void Create(int userId, string title, DateTime deadline)
    {
        if (deadline.Date < DateTime.UtcNow.Date || deadline.Date > DateTime.UtcNow.Date.AddYears(5))
            throw new ValidationException("Choose a deadline between today and five years from now.");
        if (db.UserGoals.Count(g => g.UserId == userId && !g.IsCompleted) >= 20)
            throw new ValidationException("Finish or remove an existing goal before adding more. You can have 20 active goals.");
        db.UserGoals.Add(new UserGoal { UserId = userId, GoalType = title.Trim(), Deadline = deadline.Date });
        db.SaveChanges();
    }

    public bool Toggle(int userId, int id)
    {
        var goal = db.UserGoals.FirstOrDefault(g => g.Id == id && g.UserId == userId);
        if (goal is null)
            return false;
        goal.IsCompleted = !goal.IsCompleted;
        db.SaveChanges();
        return true;
    }

    public bool Delete(int userId, int id)
    {
        var goal = db.UserGoals.FirstOrDefault(g => g.Id == id && g.UserId == userId);
        if (goal is null)
            return false;
        db.UserGoals.Remove(goal);
        db.SaveChanges();
        return true;
    }
}
