// Generates fake home meter readings, one every 2 seconds.
// Usage: dotnet run --project src/HomeGrid.Producer

using System.Text.Json;
using Confluent.Kafka;

var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "meter-readings";

string[] rooms = ["Kitchen", "Bathroom", "Living Room", "Bedroom", "Garage"];

(string Type, double MinKwh, double MaxKwh)[] circuits =
[
    ("kitchen",       0.5, 4.0),
    ("hvac",          1.5, 6.0),
    ("entertainment", 0.1, 1.5),
];

using var producer = new ProducerBuilder<Null, string>(
    new ProducerConfig { BootstrapServers = bootstrapServers }).Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Publishing home readings to '{topic}' on {bootstrapServers}");
Console.WriteLine("Press Ctrl+C to stop.\n");

while (!cts.IsCancellationRequested)
{
    var (type, minKwh, maxKwh) = circuits[Random.Shared.Next(circuits.Length)];
    var circuitId = $"CB-{Random.Shared.Next(1, 21):D2}";
    var room = rooms[Random.Shared.Next(rooms.Length)];
    var kwh = Math.Round(minKwh + Random.Shared.NextDouble() * (maxKwh - minKwh), 3);

    var reading = new Dictionary<string, object>
    {
        ["circuit_id"] = circuitId,
        ["room"] = room,
        ["type"] = type,
        ["kwh"] = kwh,
        // Same shape as Python's isoformat(), which the dashboard parses.
        ["timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz")
    };

    producer.Produce(topic, new Message<Null, string> { Value = JsonSerializer.Serialize(reading) });
    Console.WriteLine($"  → {circuitId}  {room,-15}  {type,-14}  {kwh} kWh");

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

producer.Flush(TimeSpan.FromSeconds(5));
