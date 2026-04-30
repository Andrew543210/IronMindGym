using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/profile")]
public class ProfileController(IronMindDbContext db) : ApiController
{
    [HttpPut("language")]
    public ActionResult SaveLanguage(LanguageRequest request)
    {
        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForAthletes(db, UserId);
        var profile = db.Profiles.FirstOrDefault(p => p.UserId == UserId);
        if (profile is null)
            db.Profiles.Add(profile = new Profile { UserId = UserId });
        profile.Language = request.Language;
        db.SaveChanges();
        transaction.Commit();
        return NoContent();
    }
}

public sealed record LanguageRequest(
    [Required, RegularExpression("^(en|uk)$", ErrorMessage = "Choose English or Ukrainian.")] string Language);
