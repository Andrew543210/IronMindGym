using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class GoalService(IronMindDbContext db)
{
    public List<UserGoal> List(int userId) => db.UserGoals.Where(g => g.UserId == userId)
        .OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline).ToList();

    public void Create(int userId, string title, DateOnly deadline)
    {
        var today = AthleteCalendar.For(db, userId).Today;
        if (deadline < today || deadline > today.AddYears(5))
            throw new ValidationException("Choose a deadline between today and five years from now.");
        if (db.UserGoals.Count(g => g.UserId == userId && !g.IsCompleted) >= 20)
            throw new ValidationException("Finish or remove an existing goal before adding more. You can have 20 active goals.");
        db.UserGoals.Add(new UserGoal { UserId = userId, GoalType = title.Trim(), Deadline = deadline });
        db.SaveChanges();
    }

    public bool SetCompleted(int userId, int id, bool completed) =>
        db.UserGoals.Where(g => g.Id == id && g.UserId == userId)
            .ExecuteUpdate(goal => goal.SetProperty(g => g.IsCompleted, completed)) > 0;

    public bool Delete(int userId, int id) =>
        db.UserGoals.Where(g => g.Id == id && g.UserId == userId).ExecuteDelete() > 0;
}
