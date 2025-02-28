namespace WebApi1Telemetry.Models;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    // Внешний ключ
    public int UserId { get; set; }
    public User User { get; set; }
}