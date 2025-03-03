using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApi1Telemetry.Extensions;
using WebApi1Telemetry.HttpClients;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IDistributedCache _cache;

    public UserController(ApiDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }
    
    // GET: api/user
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{id}/articles")]
    public async Task<ActionResult<IEnumerable<Article>>> GetUserArticles(int id)
    {
        var cacheKey = $"UserArticles_{id}";

        var articles = await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var user = await _context.Users
                    .Include(u => u.Articles)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found.");
                }

                return user.Articles;
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        return Ok(articles);
    }


    [HttpGet("{id}/weather")]
    public async Task<ActionResult<UserForcast>> GetUserWeather(int id, [FromServices] IWebApi2TelClient api2TelClient)
    {
        var userCacheKey = $"User_{id}";

        var user = await _cache.GetOrCreateAsync(
            userCacheKey,
            async () =>
            {
                var fetchedUser = await _context.Users.SingleOrDefaultAsync(u => u.Id == id);
                if (fetchedUser == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found.");
                }

                return fetchedUser;
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        var weatherForecast = await api2TelClient.GetResourceAsync();

        if (user == null)
        {
            return NotFound(id);
        }
        
        var userForecast = new UserForcast(user, weatherForecast);

        return Ok(userForecast);
    }
    
    public record UserForcast(User User, WeatherForecast[] Forecast);
}