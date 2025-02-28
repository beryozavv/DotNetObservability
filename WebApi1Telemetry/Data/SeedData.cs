using Microsoft.EntityFrameworkCore;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Data;

public static class SeedData
{
    public static async Task Initialize(ApiDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Users.Count() < 5)
        {
            await foreach (var contextUser in context.Users)
            {
                context.Users.Remove(contextUser);
            }
        }
        await context.SaveChangesAsync();

        if (!await context.Users.AnyAsync())
        {
            await context.Users.AddRangeAsync(
                new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
                new User { Id = 2, Name = "Bob Brown", Email = "bob@example.com" },
                new User { Id = 3, Name = "Helena Brown", Email = "helena@example.com" },
                new User { Id = 4, Name = "Dense Washington", Email = "dense@example.com" },
                new User { Id = 5, Name = "Alex Lebovsky", Email = "alex@example.com" }
            );

            await context.Articles.AddRangeAsync(
                new Article { Id = 1, Title = "Third Article", Content = "This is the third article.", UserId = 1 },
                new Article { Id = 2, Title = "Fourth Article", Content = "This is the fourth article.", UserId = 1 },
                new Article { Id = 3, Title = "First Article", Content = "This is the first article.", UserId = 2 },
                new Article { Id = 4, Title = "Second Article", Content = "This is the second article.", UserId = 2 },
                new Article { Id = 5, Title = "Fifth Article", Content = "This is the 5 article.", UserId = 3 },
                new Article { Id = 6, Title = "Sixth Article", Content = "This is the 6 article.", UserId = 3 },
                new Article { Id = 7, Title = "Seventh Article", Content = "This is the 7 article.", UserId = 3 },
                new Article { Id = 8, Title = "8 Article", Content = "This is the 8 article.", UserId = 3 },
                new Article { Id = 9, Title = "9 Article", Content = "This is the 9 article.", UserId = 5 },
                new Article { Id = 10, Title = "10 Article", Content = "This is the 10 article.", UserId = 5 }
            );

            await context.SaveChangesAsync();
        }
    }
}