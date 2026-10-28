using System.Text.Json;
using Confluent.Kafka;

namespace HomeGrid;

/// <summary>
/// Polls Kafka for meter readings in the background. The dashboard starts before the Kafka
/// sidecar or topic exist, so this keeps quietly retrying until they do.
/// </summary>
public sealed class MeterReadingConsumer(
    KafkaSettings settings,
    EventBuffer events,
    ILogger<MeterReadingConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Factory.StartNew(
            () => ConsumeLoop(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Consume(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogDebug("Kafka unavailable ({Message}) — retrying in 5s", e.Message);
            }

            stoppingToken.WaitHandle.WaitOne(RetryDelay);
        }
    }

    private void Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SocketTimeoutMs = 3000,
            // The topic is created after the dashboard starts; without this the consumer
            // would take up to 5 minutes to notice it.
            TopicMetadataRefreshIntervalMs = 5000
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            // librdkafka is noisy while the broker does not exist yet; keep the console clean.
            .SetLogHandler((_, _) => { })
            .SetErrorHandler((_, error) => logger.LogDebug("Kafka error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(settings.Topic);
        logger.LogInformation("Consumer connected — listening on topic '{Topic}'", settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(1));
                }
                catch (ConsumeException e)
                {
                    // e.g. the topic does not exist yet
                    logger.LogDebug("Poll error: {Reason}", e.Error.Reason);
                    continue;
                }

                if (result?.Message?.Value is not { } value)
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(value);
                    events.Add(doc.RootElement.Clone());
                }
                catch (JsonException)
                {
                    logger.LogWarning("Skipped undecodable message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
