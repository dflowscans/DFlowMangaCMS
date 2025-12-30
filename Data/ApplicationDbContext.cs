using Microsoft.EntityFrameworkCore;
using MangaReader.Models;

namespace MangaReader.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Manga> Mangas { get; set; } = null!;
    public DbSet<Chapter> Chapters { get; set; } = null!;
    public DbSet<ChapterPage> ChapterPages { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserBookmark> UserBookmarks { get; set; } = null!;
    public DbSet<UserRating> UserRatings { get; set; } = null!;
    public DbSet<ChapterComment> ChapterComments { get; set; } = null!;
    public DbSet<ChapterView> ChapterViews { get; set; } = null!;
    public DbSet<PfpDecoration> PfpDecorations { get; set; } = null!;
    public DbSet<UserTitle> UserTitles { get; set; } = null!;
    public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<CommentReaction> CommentReactions { get; set; } = null!;
    public DbSet<UserUnlockedDecoration> UserUnlockedDecorations { get; set; } = null!;
    public DbSet<UserUnlockedTitle> UserUnlockedTitles { get; set; } = null!;
    public DbSet<ChangelogEntry> ChangelogEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Manga configuration
        modelBuilder.Entity<Manga>()
            .HasMany(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter configuration
        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Pages)
            .WithOne(p => p.Chapter)
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter comments configuration
        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Comments)
            .WithOne(cc => cc.Chapter)
            .HasForeignKey(cc => cc.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(cc => cc.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(cc => cc.RepliedToUser)
            .WithMany()
            .HasForeignKey(cc => cc.RepliedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ChapterNumber decimal precision (supports 1, 1.1, 1.5, 10.25 etc)
        modelBuilder.Entity<Chapter>()
            .Property(c => c.ChapterNumber)
            .HasColumnType("decimal(18,2)");

        // Indices for performance
        modelBuilder.Entity<Manga>()
            .HasIndex(m => m.IsFeatured)
            .IsUnique(false);

        modelBuilder.Entity<Chapter>()
            .HasIndex(c => c.MangaId)
            .IsUnique(false);

        modelBuilder.Entity<Chapter>()
            .HasIndex(c => c.ReleasedDate)
            .IsUnique(false);

        // User-Bookmark relationship
        modelBuilder.Entity<UserBookmark>()
            .HasOne(ub => ub.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBookmark>()
            .HasOne(ub => ub.Manga)
            .WithMany()
            .HasForeignKey(ub => ub.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserRating>()
            .HasIndex(r => new { r.UserId, r.MangaId })
            .IsUnique();

        // ChapterView unique constraint
        modelBuilder.Entity<ChapterView>()
            .HasIndex(cv => new { cv.UserId, cv.ChapterId })
            .IsUnique();

        // Notification configuration
        modelBuilder.Entity<Notification>()
            .ToTable("Notifications")
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.TriggerUser)
            .WithMany()
            .HasForeignKey(n => n.TriggerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // CommentReaction configuration
        modelBuilder.Entity<CommentReaction>()
            .HasOne(cr => cr.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(cr => cr.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentReaction>()
            .HasOne(cr => cr.User)
            .WithMany(u => u.CommentReactions)
            .HasForeignKey(cr => cr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentReaction>()
            .HasIndex(cr => new { cr.CommentId, cr.UserId })
            .IsUnique();

        // UserUnlockedDecoration configuration
        modelBuilder.Entity<UserUnlockedDecoration>()
            .ToTable("UserUnlockedDecoration")
            .HasOne(ud => ud.User)
            .WithMany(u => u.UnlockedDecorations)
            .HasForeignKey(ud => ud.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserUnlockedDecoration>()
            .HasIndex(ud => new { ud.UserId, ud.DecorationId })
            .IsUnique();

        // UserUnlockedTitle configuration
        modelBuilder.Entity<UserUnlockedTitle>()
            .ToTable("UserUnlockedTitle")
            .HasOne(ut => ut.User)
            .WithMany(u => u.UnlockedTitles)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserUnlockedTitle>()
            .HasIndex(ut => new { ut.UserId, ut.TitleId })
            .IsUnique();

        // PfpDecoration seed
        modelBuilder.Entity<PfpDecoration>().HasData(
            new PfpDecoration 
            { 
                Id = 1, 
                Name = "Glow Ring", 
                ImageUrl = "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/3o7TKMGV9mGfVf8kEw/giphy.gif", 
                LevelRequirement = 1, 
                IsAnimated = true, 
                CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) 
            },
            new PfpDecoration
            {
                Id = 2,
                Name = "Golden Sparkle",
                ImageUrl = "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/26hpKz786Cq0Y/giphy.gif",
                LevelRequirement = 5,
                IsAnimated = true,
                CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default admin user
        SeedDefaultAdmin(modelBuilder);
    }

    private static void SeedDefaultAdmin(ModelBuilder modelBuilder)
    {
        // Pre-computed hash for password "Admin" using BCrypt
        // This is static to avoid generating different hashes on each build
        string passwordHash = "$2a$11$yNru262Z6gimpMKoGk0MpOYyn4jijDcLpHruW1.VtclJAsIAwg2mq";
        
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "Admin",
                PasswordHash = passwordHash,
                IsAdmin = true,
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
         );

        // UserTitle seed
        modelBuilder.Entity<UserTitle>().HasData(
            new UserTitle { Id = 1, Name = "Novice Reader", Color = "#94a3b8", LevelRequirement = 1, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) },
            new UserTitle { Id = 2, Name = "Manga Enthusiast", Color = "#3b82f6", LevelRequirement = 5, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) },
            new UserTitle { Id = 3, Name = "Legendary Scholar", Color = "#f59e0b", LevelRequirement = 10, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
