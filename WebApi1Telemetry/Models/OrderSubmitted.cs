namespace WebApi1Telemetry.Models;

// 3. Создайте класс сообщения
public class OrderSubmitted
{
    public Guid OrderId { get; set; }
    public string? FeedbackEmail { get; set; } = null!;
    public string? Submitter { get; set; }
}