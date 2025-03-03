namespace WebApi1Telemetry.Settings;

public class RabbitMqSettings
{
    public string Host { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; }
    public string Password { get; set; }
    public ushort Port { get; set; } = 5672;
    public bool UseSsl { get; set; } = false;
}

// Класс для хранения настроек MassTransit

// Настройки для механизма повторных попыток