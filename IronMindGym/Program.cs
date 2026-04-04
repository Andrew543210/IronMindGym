using System.Text.Json.Serialization;
using IronMindGym.Data;
using IronMindGym.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<IronMindDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection to a PostgreSQL database. See README for the local Docker database."),
    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IronMindDbContext>();
    DatabaseMigrations.Apply(db);
    DataSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.MapControllers();
app.Run();

public partial class Program;
