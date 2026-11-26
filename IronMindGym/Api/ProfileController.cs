using System.Text.Json;
using IronMindGym.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IronMindGym.Api;

[Route("api/profile")]
public class ProfileController(ProfileService profiles) : ApiController
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [HttpGet]
    public ProfileOverview Overview() => profiles.BuildOverview(UserId);

    [HttpGet("settings")]
    public ProfileSettingsResponse Settings() => profiles.Settings(UserId);

    [HttpPut("settings")]
    public ActionResult SaveSettings(ProfileSettings request)
    {
        profiles.Update(UserId, request);
        return NoContent();
    }

    [HttpPut("password")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
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
        profiles.SaveLanguage(UserId, request.Language);
        return NoContent();
    }
}
