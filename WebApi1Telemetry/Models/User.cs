namespace WebApi1Telemetry.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }

    // Навигационное свойство
    public ICollection<Article> Articles { get; set; }
}