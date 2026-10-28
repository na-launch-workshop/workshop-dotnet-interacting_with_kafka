// Creates the meter-readings topic. The Kafka sidecar has auto-create turned off.
// Usage: dotnet run --project src/HomeGrid.CreateTopic

using Confluent.Kafka;
using Confluent.Kafka.Admin;

var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "meter-readings";

using var admin = new AdminClientBuilder(
    new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

try
{
    await admin.CreateTopicsAsync(
        [new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);
    Console.WriteLine($"Topic '{topic}' created on {bootstrapServers}");
}
catch (CreateTopicsException e) when (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
{
    Console.WriteLine($"Topic '{topic}' already exists on {bootstrapServers}");
}
