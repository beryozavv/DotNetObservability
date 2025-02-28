namespace WebApi1Telemetry.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; }
    public string Email { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? FeedbackEmail { get; set; } = null!;
    public string? Submitter { get; set; }
    public string? OrderMarker { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}