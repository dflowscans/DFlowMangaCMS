using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MangaReader.Data;
using MangaReader.Models;
using System.Security.Claims;
using System.Xml.Linq;

namespace MangaReader.Controllers;

public class AuthController(ApplicationDbContext context, ILogger<AuthController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View();
        }

        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", username);
            }
            return View();
        }

        // Create authentication claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("IsSubAdmin", user.IsSubAdmin.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("User {Username} logged in successfully.", username);
        }

        // Redirect to admin if admin user, otherwise to home
        return user.IsAdmin ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        if (username.Length < 3 || username.Length > 100)
        {
            ModelState.AddModelError(nameof(username), "Username must be between 3 and 100 characters.");
            return View();
        }

        if (password.Length < 6)
        {
            ModelState.AddModelError(nameof(password), "Password must be at least 6 characters.");
            return View();
        }

        if (_context.Users.Any(u => u.Username == username))
        {
            ModelState.AddModelError(nameof(username), "Username already exists.");
            return View();
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("New user registered: {Username}", username);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction(nameof(Login));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            ModelState.AddModelError(string.Empty, "All fields are required.");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "New passwords do not match.");
            return View();
        }

        if (newPassword.Length < 6)
        {
            ModelState.AddModelError(nameof(newPassword), "New password must be at least 6 characters.");
            return View();
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = _context.Users.Find(userId);

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(currentPassword), "Current password is incorrect.");
            return View();
        }

        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(newPassword), "New password cannot be the same as the current password.");
            return View();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("User {Username} changed password.", user.Username);
        }
        TempData["SuccessMessage"] = "Password changed successfully.";

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> PublicProfile(int id)
    {
        var user = await _context.Users
            .Include(u => u.EquippedDecoration)
            .Include(u => u.EquippedTitle)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Get reading stats
        var bookmarks = await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == id)
            .ToListAsync();

        ViewBag.User = user;
        ViewBag.Bookmarks = user.HideReadingList ? [] : bookmarks;
        ViewBag.ReadingStats = new
        {
            Total = bookmarks.Count,
            Reading = bookmarks.Count(b => b.Status == BookmarkStatus.Reading),
            Completed = bookmarks.Count(b => b.Status == BookmarkStatus.Completed),
            OnHold = bookmarks.Count(b => b.Status == BookmarkStatus.OnHold),
            Dropped = bookmarks.Count(b => b.Status == BookmarkStatus.Dropped),
            PlanToRead = bookmarks.Count(b => b.Status == BookmarkStatus.PlanToRead)
        };

        // Total comments
        ViewBag.TotalComments = await _context.ChapterComments.CountAsync(c => c.UserId == id);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile(string? status = null)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users
            .Include(u => u.EquippedDecoration)
            .Include(u => u.EquippedTitle)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Calculate XP progress
        int currentLevelXp = (int)(100 * Math.Pow(1.5, user.Level - 1));
        ViewBag.XpProgress = (double)user.XP / currentLevelXp * 100;
        ViewBag.XpToNextLevel = currentLevelXp;

        // Load site settings for decorations and titles with fallback
        try
        {
            ViewBag.EnableDecorations = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableDecorations"))?.Value ?? "true";
            ViewBag.EnableTitles = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableTitles"))?.Value ?? "true";
        }
        catch
        {
            ViewBag.EnableDecorations = "true";
            ViewBag.EnableTitles = "true";
        }

        // Normalize status
        status = status?.Trim();

        ViewBag.User = user;
        ViewBag.AvailableDecorations = await _context.PfpDecorations.OrderBy(d => d.LevelRequirement).ToListAsync();
        ViewBag.AvailableTitles = await _context.UserTitles.OrderBy(t => t.LevelRequirement).ToListAsync();

        // Get unlocked items
        ViewBag.UnlockedDecorations = await _context.Set<UserUnlockedDecoration>()
            .Where(ud => ud.UserId == userId)
            .Select(ud => ud.DecorationId)
            .ToListAsync();
        ViewBag.UnlockedTitles = await _context.Set<UserUnlockedTitle>()
            .Where(ut => ut.UserId == userId)
            .Select(ut => ut.TitleId)
            .ToListAsync();

        // Get bookmarks with manga details (strongly typed)
        var bookmarksQuery = _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == userId);

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookmarkStatus>(status, true, out var bookmarkStatus))
        {
            bookmarksQuery = bookmarksQuery.Where(b => b.Status == bookmarkStatus);
        }

        var bookmarks = bookmarksQuery
            .OrderByDescending(b => b.UpdatedAt)
            .ToList();

        ViewBag.User = user;
        ViewBag.Bookmarks = bookmarks;
        ViewBag.CurrentStatus = status;
        ViewBag.AllCount = _context.UserBookmarks.Count(b => b.UserId == userId);
        ViewBag.PlanToReadCount = _context.UserBookmarks.Count(b => b.UserId == userId && b.Status == BookmarkStatus.PlanToRead);
        ViewBag.ReadingCount = _context.UserBookmarks.Count(b => b.UserId == userId && b.Status == BookmarkStatus.Reading);
        ViewBag.CompletedCount = _context.UserBookmarks.Count(b => b.UserId == userId && b.Status == BookmarkStatus.Completed);
        ViewBag.OnHoldCount = _context.UserBookmarks.Count(b => b.UserId == userId && b.Status == BookmarkStatus.OnHold);
        ViewBag.DroppedCount = _context.UserBookmarks.Count(b => b.UserId == userId && b.Status == BookmarkStatus.Dropped);

        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportAniListXml()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var bookmarks = await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == userId)
            .ToListAsync();

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("myanimelist",
                new XElement("myinfo",
                    new XElement("user_id", user.Id),
                    new XElement("user_name", user.Username),
                    new XElement("user_export_type", "2"),
                    new XElement("user_total_manga", bookmarks.Count),
                    new XElement("user_total_reading", bookmarks.Count(b => b.Status == BookmarkStatus.Reading)),
                    new XElement("user_total_completed", bookmarks.Count(b => b.Status == BookmarkStatus.Completed)),
                    new XElement("user_total_onhold", bookmarks.Count(b => b.Status == BookmarkStatus.OnHold)),
                    new XElement("user_total_dropped", bookmarks.Count(b => b.Status == BookmarkStatus.Dropped)),
                    new XElement("user_total_plantoread", bookmarks.Count(b => b.Status == BookmarkStatus.PlanToRead))
                ),
                bookmarks.Select(b => new XElement("manga",
                    new XElement("manga_mangadb_id", b.Manga?.AniListId ?? 0),
                    new XElement("series_title", new XCData(b.Manga?.Title ?? "")),
                    new XElement("series_type", "Manga"),
                    new XElement("series_chapters", "0"),
                    new XElement("my_id", "0"),
                    new XElement("my_read_chapters", "0"),
                    new XElement("my_status", b.Status switch {
                        BookmarkStatus.Reading => "Reading",
                        BookmarkStatus.Completed => "Completed",
                        BookmarkStatus.OnHold => "On-Hold",
                        BookmarkStatus.Dropped => "Dropped",
                        BookmarkStatus.PlanToRead => "Plan to Read",
                        _ => "Reading"
                    }),
                    new XElement("my_last_updated", ((DateTimeOffset)b.UpdatedAt).ToUnixTimeSeconds())
                ))
            )
        );

        var bytes = System.Text.Encoding.UTF8.GetBytes(xml.ToString());
        return File(bytes, "application/xml", $"{user.Username}_AniList_Export.xml");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ImportAniListXml(IFormFile xmlFile)
    {
        if (xmlFile == null || xmlFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid XML file.";
            return RedirectToAction(nameof(Profile));
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        try
        {
            using var stream = xmlFile.OpenReadStream();
            var doc = XDocument.Load(stream);
            var mangaElements = doc.Root?.Elements("manga") ?? [];
            
            int importedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var element in mangaElements)
            {
                var aniListIdStr = element.Element("manga_mangadb_id")?.Value;
                var title = element.Element("series_title")?.Value;
                var statusStr = element.Element("my_status")?.Value;

                if (string.IsNullOrEmpty(title)) continue;

                // Try to find manga in our database
                Manga? manga = null;
                if (int.TryParse(aniListIdStr, out int aniListId) && aniListId > 0)
                {
                    manga = await _context.Mangas.FirstOrDefaultAsync(m => m.AniListId == aniListId);
                }

                manga ??= await _context.Mangas.FirstOrDefaultAsync(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase));

                if (manga == null)
                {
                    skippedCount++;
                    continue;
                }

                // Map status
                BookmarkStatus status = statusStr switch
                {
                    "Reading" => BookmarkStatus.Reading,
                    "Completed" => BookmarkStatus.Completed,
                    "On-Hold" => BookmarkStatus.OnHold,
                    "Dropped" => BookmarkStatus.Dropped,
                    "Plan to Read" => BookmarkStatus.PlanToRead,
                    _ => BookmarkStatus.Reading
                };

                // Update or create bookmark
                var existing = await _context.UserBookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == manga.Id);

                if (existing != null)
                {
                    existing.Status = status;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
                else
                {
                    _context.UserBookmarks.Add(new UserBookmark
                    {
                        UserId = userId,
                        MangaId = manga.Id,
                        Status = status,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    importedCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Import completed! {importedCount} added, {updatedCount} updated, {skippedCount} skipped (manga not found in our database).";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error importing XML: " + ex.Message;
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false });

        return Json(new { 
            success = true, 
            level = user.Level, 
            xp = user.XP, 
            xpNeeded = (int)(100 * Math.Pow(1.5, user.Level - 1))
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EquipDecoration([FromBody] DecorationRequest request)
    {
        if (request == null) return Json(new { success = false, message = "Invalid request" });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, message = "User not found" });

        if (request.DecorationId <= 0)
        {
            user.EquippedDecorationId = null;
        }
        else
        {
            var decoration = await _context.PfpDecorations.FindAsync(request.DecorationId);
            if (decoration == null) return Json(new { success = false, message = "Decoration not found" });

            // Check if user has unlocked it (awarded)
            var isUnlocked = await _context.Set<UserUnlockedDecoration>()
                .AnyAsync(ud => ud.UserId == userId && ud.DecorationId == request.DecorationId);

            // Allow if level requirement met OR if explicitly unlocked (awarded)
            bool canEquip = (user.Level >= decoration.LevelRequirement && !decoration.IsLocked) || isUnlocked;

            if (!canEquip)
            {
                if (decoration.IsLocked && !isUnlocked)
                {
                    return Json(new { success = false, message = "This decoration is locked and you haven't unlocked it yet!" });
                }
                return Json(new { success = false, message = $"You need to be level {decoration.LevelRequirement} to equip this!" });
            }
            
            user.EquippedDecorationId = request.DecorationId;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Decoration equipped!" });
    }

    public class DecorationRequest
    {
        public int DecorationId { get; set; }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EquipTitle([FromBody] TitleRequest request)
    {
        if (request == null) return Json(new { success = false, message = "Invalid request" });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, message = "User not found" });

        if (request.TitleId <= 0)
        {
            user.EquippedTitleId = null;
        }
        else
        {
            var title = await _context.UserTitles.FindAsync(request.TitleId);
            if (title == null) return Json(new { success = false, message = "Title not found" });

            // Check if user has unlocked it (awarded)
            var isUnlocked = await _context.Set<UserUnlockedTitle>()
                .AnyAsync(ut => ut.UserId == userId && ut.TitleId == request.TitleId);

            // Allow if level requirement met OR if explicitly unlocked (awarded)
            bool canEquip = (user.Level >= title.LevelRequirement && !title.IsLocked) || isUnlocked;

            if (!canEquip)
            {
                if (title.IsLocked && !isUnlocked)
                {
                    return Json(new { success = false, message = "This title is locked and you haven't unlocked it yet!" });
                }
                return Json(new { success = false, message = $"You need to be level {title.LevelRequirement} to equip this!" });
            }
            
            user.EquippedTitleId = request.TitleId;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Title equipped!" });
    }

    public class TitleRequest
    {
        public int TitleId { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(IFormFile? avatar, bool hideReadingList)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (avatar != null && avatar.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Invalid avatar format.";
                return RedirectToAction(nameof(Profile));
            }
            if (avatar.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Avatar too large (max 5MB).";
                return RedirectToAction(nameof(Profile));
            }

            var folder = Path.Combine("wwwroot", "uploads", "avatars", userId.ToString());
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var fileName = $"avatar_{DateTime.UtcNow.Ticks}{ext}";
            var path = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(path, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            user.AvatarUrl = $"/uploads/avatars/{userId}/{fileName}";
        }

        user.HideReadingList = hideReadingList;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }
}
