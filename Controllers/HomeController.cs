using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MangaReader.Data;
using MangaReader.Models;

namespace MangaReader.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var manga = await _context.Mangas
                .Include(m => m.Chapters)
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();

            // Load banner shadow settings
            try
            {
                ViewBag.EnableBannerShadow = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableBannerShadow"))?.Value ?? "false";
                ViewBag.BannerShadowStrength = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowStrength"))?.Value ?? "0.8";
                ViewBag.BannerShadowDepth = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowDepth"))?.Value ?? "4";
            }
            catch
            {
                ViewBag.EnableBannerShadow = "false";
                ViewBag.BannerShadowStrength = "0.8";
                ViewBag.BannerShadowDepth = "4";
            }

            return View(manga);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page. Database may need fixing.");
            // If the error is about missing columns, the user might need to run the fix
            if (ex.Message.Contains("AniListId") || ex.Message.Contains("Unknown column"))
            {
                ViewBag.DatabaseError = true;
            }
            return View(new List<Manga>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Changelog()
    {
        var entries = await _context.ChangelogEntries
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        if (User.Identity?.IsAuthenticated ?? false)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
                var user = await _context.Users.FindAsync(userId);
                ViewBag.FollowingChangelog = user?.FollowChangelog ?? false;
            }
        }

        return View(entries);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFollowChangelog()
    {
        if (!(User.Identity?.IsAuthenticated ?? false)) return Unauthorized();

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FollowChangelog = !user.FollowChangelog;
        await _context.SaveChangesAsync();

        return Json(new { success = true, following = user.FollowChangelog });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}