using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/profile")]
public class ProfileController(IronMindDbContext db, ProfileService profiles) : ApiController
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [HttpGet]
    public ProfileOverview Overview() => profiles.BuildOverview(UserId);

    [HttpGet("settings")]
    public ProfileSettingsResponse Settings() => new(
        profiles.BuildEdit(UserId),
        db.SplitTypes.AsNoTracking().OrderBy(s => s.Id).Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk))).ToList());

    [HttpPut("settings")]
    public ActionResult SaveSettings(ProfileSettings request)
    {
        try
        {
            profiles.Update(UserId, request);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            return Rejected("This username is already taken.", nameof(request.Username));
        }
        return NoContent();
    }

    [HttpPut("password")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
    {
        profiles.ChangePassword(UserId, request);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPut("weights/{date}")]
    public ActionResult LogWeight(DateOnly date, WeightRequest request)
    {
        profiles.LogWeight(UserId, date, request.WeightKg);
        return NoContent();
    }

    [HttpGet("export")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public FileContentResult Export() =>
        File(JsonSerializer.SerializeToUtf8Bytes(profiles.Export(UserId), ExportJson), "application/json", "ironmind-export.json");

    [HttpPut("language")]
    public ActionResult SaveLanguage(LanguageRequest request)
    {
        using var transaction = WriteLock.BeginForAthletes(db, UserId);
        var profile = db.Profiles.FirstOrDefault(p => p.UserId == UserId);
        if (profile is null)
            db.Profiles.Add(profile = new Profile { UserId = UserId });
        profile.Language = request.Language;
        db.SaveChanges();
        transaction.Commit();
        return NoContent();
    }
}

public sealed record ProfileSettingsResponse(ProfileSettings Settings, List<OptionResponse> SplitTypes);

public sealed record WeightRequest([Range(30, 250, ErrorMessage = "Enter a weight between 30 and 250 kg.")] double WeightKg);

public sealed record LanguageRequest(
    [Required, RegularExpression("^(en|uk)$", ErrorMessage = "Choose English or Ukrainian.")] string Language);
