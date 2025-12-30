using MangaReader.Data;
using MangaReader.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST");
if (string.IsNullOrWhiteSpace(mysqlHost))
{
    mysqlHost = "5.175.192.162";
    Environment.SetEnvironmentVariable("MYSQL_HOST", mysqlHost);
}

var mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT");
if (string.IsNullOrWhiteSpace(mysqlPort))
{
    mysqlPort = "3306";
    Environment.SetEnvironmentVariable("MYSQL_PORT", mysqlPort);
}

var mysqlDb = Environment.GetEnvironmentVariable("MYSQL_DB");
if (string.IsNullOrWhiteSpace(mysqlDb))
{
    mysqlDb = "s14_DFlowScans";
    Environment.SetEnvironmentVariable("MYSQL_DB", mysqlDb);
}

var mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");
if (string.IsNullOrWhiteSpace(mysqlUser))
{
    mysqlUser = "u14_FI3QF79zRV";
    Environment.SetEnvironmentVariable("MYSQL_USER", mysqlUser);
}

var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
if (string.IsNullOrWhiteSpace(mysqlPassword))
{
    mysqlPassword = "9@8z=a4WrE=6oToZrKBN@4Zv";
    Environment.SetEnvironmentVariable("MYSQL_PASSWORD", mysqlPassword);
}

var connectionString =
    $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDb};User={mysqlUser};Password={mysqlPassword};" +
    "TreatTinyAsBoolean=true;AllowPublicKeyRetrieval=True;SslMode=Preferred";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 29))));

// Increase file upload limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

// Register application services
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Add Razor Pages support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Database initialization/migration helper
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        using var command = context.Database.GetDbConnection().CreateCommand();
        await context.Database.OpenConnectionAsync();
        
        // Ensure AniListId column exists in Mangas table
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
        var result = await command.ExecuteScalarAsync();
        if (Convert.ToInt32(result) == 0)
        {
            command.CommandText = "ALTER TABLE Mangas ADD COLUMN AniListId INT NULL;";
            await command.ExecuteNonQueryAsync();
        }

        // Ensure Title column in Chapters table is nullable
        command.CommandText = "ALTER TABLE Chapters MODIFY COLUMN Title VARCHAR(300) NULL;";
        await command.ExecuteNonQueryAsync();

        // 8. Add RepliedToUserId to ChapterComments
         try
         {
             command.CommandText = "ALTER TABLE ChapterComments ADD COLUMN RepliedToUserId INT NULL;";
             await command.ExecuteNonQueryAsync();
             command.CommandText = "ALTER TABLE ChapterComments ADD CONSTRAINT FK_ChapterComments_Users_RepliedToUserId FOREIGN KEY (RepliedToUserId) REFERENCES Users(Id);";
             await command.ExecuteNonQueryAsync();
         }
         catch { /* Column might already exist */ }

          // 9. Add ChangelogEntries table
          command.CommandText = @"
              CREATE TABLE IF NOT EXISTS ChangelogEntries (
                  Id INT AUTO_INCREMENT PRIMARY KEY,
                  Title VARCHAR(200) NOT NULL,
                  Content TEXT NOT NULL,
                  CreatedAt DATETIME NOT NULL
              );";
          await command.ExecuteNonQueryAsync();

          // 10. Add FollowChangelog to Users table
          try
          {
              command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
              var followColResult = await command.ExecuteScalarAsync();
              if (Convert.ToInt32(followColResult) == 0)
              {
                  command.CommandText = "ALTER TABLE Users ADD COLUMN FollowChangelog TINYINT(1) NOT NULL DEFAULT 1;";
                  await command.ExecuteNonQueryAsync();
              }
          }
          catch { /* Might already exist */ }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages(); // Map Razor Pages endpoints
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    ;

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch
    {
    }
}

app.Run();
