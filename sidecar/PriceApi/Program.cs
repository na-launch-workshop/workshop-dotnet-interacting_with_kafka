// Electricity price API: returns the current rate based on time of day (UTC).

var app = WebApplication.Create(args);

(int Start, int End, string Tier, double Price)[] schedule =
[
    (0,  6,  "Off-Peak", 0.079),
    (6,  16, "Mid-Peak", 0.124),
    (16, 21, "Peak",     0.189),
    (21, 22, "Mid-Peak", 0.124),
    (22, 24, "Off-Peak", 0.079),
];

app.MapGet("/price", (HttpResponse response) =>
{
    var now = DateTimeOffset.UtcNow;
    var (_, _, tier, price) = schedule.First(s => s.Start <= now.Hour && now.Hour < s.End);

    response.Headers.CacheControl = "no-store";
    return Results.Json(new
    {
        price,
        tier,
        currency = "USD",
        unit = "kWh",
        timestamp = now.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz")
    });
});

app.Run("http://0.0.0.0:5000");
