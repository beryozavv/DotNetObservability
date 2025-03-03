namespace WebApi1Telemetry.Settings;

public class RedisSettings
{
    public string Host { get; set; } = null!;
    public ushort Port { get; set; }
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
}