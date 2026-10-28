using System.Text.Json;
using System.Text.Json.Serialization;
using HomeGrid;
using Microsoft.Data.Sqlite;

const string priceUrl = "http://localhost:5000/price";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(KafkaSettings.From(builder.Configuration));
builder.Services.AddSingleton<EventBuffer>();
builder.Services.AddHostedService<MeterReadingConsumer>();
builder.Services.AddHttpClient("price", client => client.Timeout = TimeSpan.FromSeconds(2));

var app = builder.Build();

var dbPath = Path.Combine(RepoRoot.Find(), "data", "appliances.db");

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");
api.AddEndpointFilter(async (context, next) =>
{
    context.HttpContext.Response.Headers.CacheControl = "no-store";
    return await next(context);
});

api.MapGet("/events", (EventBuffer events) => Results.Json(events.Snapshot()));

api.MapGet("/appliances", () =>
{
    try
    {
        // Read-only, so a missing database stays missing instead of becoming an empty file,
        // and no pooling, so the file is not held open while you edit it in SQLite Explorer.
        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name, daily_kwh FROM appliances ORDER BY daily_kwh DESC";
        using var reader = cmd.ExecuteReader();

        // Values are passed through as stored, so a row someone typed in by hand with an odd
        // value shows up as-is instead of blanking the whole panel.
        var rows = new List<Appliance>();
        while (reader.Read())
        {
            rows.Add(new Appliance(Value(reader, 0), Value(reader, 1)));
        }

        return Results.Json(rows);
    }
    catch (Exception)
    {
        return Results.Json(Array.Empty<Appliance>());
    }
});

api.MapGet("/price", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var json = await httpClientFactory.CreateClient("price").GetStringAsync(priceUrl);
        return Results.Json(JsonSerializer.Deserialize<JsonElement>(json));
    }
    catch (Exception)
    {
        return Results.Json<object?>(null);
    }
});

app.Run();

static object? Value(SqliteDataReader reader, int ordinal) =>
    reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);

internal sealed record Appliance(
    [property: JsonPropertyName("name")] object? Name,
    [property: JsonPropertyName("daily_kwh")] object? DailyKwh);

public partial class Program;
