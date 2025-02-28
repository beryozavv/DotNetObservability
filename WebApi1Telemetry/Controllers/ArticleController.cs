using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticleController : ControllerBase
{
    private readonly ApiDbContext _context;

    public ArticleController(ApiDbContext context)
    {
        _context = context;
    }

    // GET: api/article
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Article>>> GetArticles()
    {
        return await _context.Articles.ToListAsync();
    }
}