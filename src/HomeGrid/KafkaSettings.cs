namespace HomeGrid;

/// <summary>
/// Read from the same KAFKA_* environment variables the devfile sets, so the dashboard,
/// producer and topic tool all agree on where Kafka is.
/// </summary>
public sealed record KafkaSettings(string BootstrapServers, string Topic, string GroupId)
{
    public static KafkaSettings From(IConfiguration config) => new(
        config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092",
        config["KAFKA_TOPIC"] ?? "meter-readings",
        config["KAFKA_GROUP_ID"] ?? "grid-dashboard");
}
