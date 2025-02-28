namespace WebApi1Telemetry.Settings;

public class RetrySettings
{
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalSeconds { get; set; } = 5;
}