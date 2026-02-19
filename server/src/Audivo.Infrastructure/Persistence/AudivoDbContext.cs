using Audivo.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Audivo.Infrastructure.Persistence;

public class AudivoDbContext : IdentityDbContext<ApplicationUser>
{
    public AudivoDbContext(DbContextOptions<AudivoDbContext> options) : base(options)
    {
    }

    public DbSet<Audiobook> Audiobooks => Set<Audiobook>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<UserLibrary> UserLibraries => Set<UserLibrary>();

    public DbSet<ListeningProgress> ListeningProgressRecords => Set<ListeningProgress>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Audiobook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Author).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Genre).HasMaxLength(200);
            entity.Property(e => e.Narrator).HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.CoverImageUrl).HasMaxLength(2048);
            entity.Property(e => e.UploadedByUserId).IsRequired();
            entity.HasOne(e => e.UploadedBy)
                  .WithMany(u => u.UploadedAudiobooks)
                  .HasForeignKey(e => e.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AudioFileUrl).HasMaxLength(2048).IsRequired();
            entity.HasOne(e => e.Audiobook)
                  .WithMany(a => a.Chapters)
                  .HasForeignKey(e => e.AudiobookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserLibrary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.AudiobookId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserLibraries)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Audiobook)
                  .WithMany()
                  .HasForeignKey(e => e.AudiobookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ListeningProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.AudiobookId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ListeningProgressRecords)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Audiobook)
                  .WithMany()
                  .HasForeignKey(e => e.AudiobookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CurrentChapter)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentChapterId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(512).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
