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
    public Task<ProfileOverview> Overview(CancellationToken cancellationToken) => profiles.BuildOverviewAsync(UserId, cancellationToken);

    [HttpGet("settings")]
    public Task<ProfileSettingsResponse> Settings(CancellationToken cancellationToken) => profiles.SettingsAsync(UserId, cancellationToken);

    [HttpPut("settings")]
    public async Task<ActionResult> SaveSettings(ProfileSettings request, CancellationToken cancellationToken)
    {
        await profiles.UpdateAsync(UserId, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("password")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await profiles.ChangePasswordAsync(UserId, request, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPut("weights/{date}")]
    public async Task<ActionResult> LogWeight(DateOnly date, WeightRequest request, CancellationToken cancellationToken)
    {
        await profiles.LogWeightAsync(UserId, date, request.WeightKg, cancellationToken);
        return NoContent();
    }

    [HttpGet("export")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<FileContentResult> Export(CancellationToken cancellationToken) =>
        File(JsonSerializer.SerializeToUtf8Bytes(await profiles.ExportAsync(UserId, cancellationToken), ExportJson), "application/json", "ironmind-export.json");

    [HttpPut("language")]
    public async Task<ActionResult> SaveLanguage(LanguageRequest request, CancellationToken cancellationToken)
    {
        await profiles.SaveLanguageAsync(UserId, request.Language!.Value, cancellationToken);
        return NoContent();
    }
}
