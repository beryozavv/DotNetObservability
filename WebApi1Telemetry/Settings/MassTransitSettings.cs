namespace WebApi1Telemetry.Settings;

public class MassTransitSettings
{
    public string OrderSubmittedQueueName { get; set; } = "order-submitted-queue";
    public int PrefetchCount { get; set; } = 16;
    public int ConcurrentMessageLimit { get; set; } = 4;
    public RetrySettings Retry { get; set; } = new RetrySettings();
}