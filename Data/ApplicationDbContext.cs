using Microsoft.EntityFrameworkCore;
using MangaReader.Models;

namespace MangaReader.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Manga> Mangas { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<ChapterPage> ChapterPages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<ChapterComment> ChapterComments { get; set; }
    public DbSet<CommentReaction> CommentReactions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserTitle> UserTitles { get; set; }
    public DbSet<PfpDecoration> PfpDecorations { get; set; }
    public DbSet<UserUnlockedTitle> UserUnlockedTitles { get; set; }
    public DbSet<UserUnlockedDecoration> UserUnlockedDecorations { get; set; }
    public DbSet<ChangelogEntry> ChangelogEntries { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Manga - Chapter relationship
        modelBuilder.Entity<Chapter>()
            .HasOne(c => c.Manga)
            .WithMany(m => m.Chapters)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter - ChapterPage relationship
        modelBuilder.Entity<ChapterPage>()
            .HasOne(p => p.Chapter)
            .WithMany(c => c.Pages)
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // User - Bookmark relationship
        modelBuilder.Entity<Bookmark>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Bookmark>()
            .HasOne(b => b.Manga)
            .WithMany()
            .HasForeignKey(b => b.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // ChapterComment configuration
        modelBuilder.Entity<ChapterComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(c => c.Chapter)
            .WithMany(ch => ch.Comments)
            .HasForeignKey(c => c.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // CommentReaction configuration
        modelBuilder.Entity<CommentReaction>()
            .HasOne(r => r.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentReaction>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification configuration
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Seed PfpDecorations
        SeedPfpDecorations(modelBuilder);
        
        // Seed UserTitles
        SeedUserTitles(modelBuilder);
    }

    private static void SeedPfpDecorations(ModelBuilder modelBuilder)
    {
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
    }

    private static void SeedUserTitles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTitle>().HasData(
            new UserTitle { Id = 1, Name = "Novice Reader", Color = "#94a3b8", LevelRequirement = 1, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) },
            new UserTitle { Id = 2, Name = "Manga Enthusiast", Color = "#3b82f6", LevelRequirement = 5, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) },
            new UserTitle { Id = 3, Name = "Legendary Scholar", Color = "#f59e0b", LevelRequirement = 10, CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
