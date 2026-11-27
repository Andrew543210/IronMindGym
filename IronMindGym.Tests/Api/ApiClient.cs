using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IronMindGym.Api;

namespace IronMindGym.Tests.Api;

public sealed class ApiClient(HttpClient http) : IDisposable
{
    public const string Password = "TrainingPassword123!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private string? csrf;

    public HttpClient Http => http;

    public async Task<T> Get<T>(string url)
    {
        var response = await http.GetAsync(url);
        await Ok(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public Task<HttpResponseMessage> Post(string url, object? body = null) => Send(HttpMethod.Post, url, body);

    public Task<HttpResponseMessage> Put(string url, object? body = null) => Send(HttpMethod.Put, url, body);

    public Task<HttpResponseMessage> Delete(string url) => Send(HttpMethod.Delete, url, null);

    public async Task<HttpResponseMessage> Send(HttpMethod method, string url, object? body)
    {
        csrf ??= (await Get<JsonElement>("/api/session/csrf")).GetProperty("token").GetString();
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        request.Headers.Add(CsrfProtectionFilter.HeaderName, csrf);
        var response = await http.SendAsync(request);
        if (url.StartsWith("/api/session/", StringComparison.Ordinal) || url == "/api/profile/password")
            csrf = null;
        return response;
    }

    public async Task Register(string username = "athlete", string? timeZone = null)
    {
        var response = await Post("/api/session/register", new
        {
            username,
            email = $"{username}@example.com",
            age = 24,
            password = Password,
            confirmPassword = Password,
            timeZone
        });
        await Ok(response);
    }

    public async Task SignIn(string email, string password = Password) =>
        await Ok(await Post("/api/session/login", new { email, password }));

    public async Task RegisterWithSetup(string username = "athlete", int splitTypeId = 1)
    {
        await Register(username);
        await Ok(await Post("/api/setup", new
        {
            weightKg = 80,
            heightCm = 180,
            benchPressKg = 60,
            squatKg = 80,
            deadliftKg = 100,
            workoutsPerWeek = 3,
            splitTypeId
        }));
    }

    public static async Task Ok(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    public static async Task<string[]> Errors(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return problem.GetProperty("errors").EnumerateObject()
            .SelectMany(field => field.Value.EnumerateArray().Select(message => message.GetString()!))
            .ToArray();
    }

    public void Dispose() => http.Dispose();
}
