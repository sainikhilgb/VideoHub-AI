using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Media;

namespace VideoHub.Api.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<CaptionFile> CaptionFiles => Set<CaptionFile>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CombinedMedia> CombinedMediaFiles => Set<CombinedMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(project => project.User)
                .WithMany()
                .HasForeignKey(project => project.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.Property(mediaFile => mediaFile.Type)
                .HasMaxLength(30);

            entity.Property(mediaFile => mediaFile.Status)
                .HasMaxLength(50);

            entity.Property(mediaFile => mediaFile.OriginalFileName)
                .HasMaxLength(255);

            entity.Property(mediaFile => mediaFile.StoredFileName)
                .HasMaxLength(255);

            entity.Property(mediaFile => mediaFile.Bucket)
                .HasMaxLength(100);

            entity.Property(mediaFile => mediaFile.StoragePath)
                .HasMaxLength(2048);

            entity.Property(mediaFile => mediaFile.Extension)
                .HasMaxLength(20);

            entity.HasIndex(mediaFile => new { mediaFile.ProjectId, mediaFile.UploadedAt });
            entity.HasIndex(mediaFile => mediaFile.StoragePath)
                .IsUnique();

            entity.HasOne(mediaFile => mediaFile.Project)
                .WithMany(project => project.MediaFiles)
                .HasForeignKey(mediaFile => mediaFile.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasIndex(transcript => new { transcript.ProjectId, transcript.Language, transcript.Version })
                .IsUnique();

            entity.HasOne(transcript => transcript.Project)
                .WithMany(project => project.Transcripts)
                .HasForeignKey(transcript => transcript.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });



        modelBuilder.Entity<Speaker>(entity =>
        {
            entity.HasOne(speaker => speaker.Transcript)
                .WithMany(transcript => transcript.Speakers)
                .HasForeignKey(speaker => speaker.TranscriptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(speaker => new { speaker.TranscriptId, speaker.SpeakerLabel })
                .IsUnique();
        });

        modelBuilder.Entity<Translation>(entity =>
        {
            entity.HasIndex(translation => new { translation.TranscriptId, translation.Language })
                .IsUnique();

            entity.HasOne(translation => translation.Transcript)
                .WithMany(transcript => transcript.Translations)
                .HasForeignKey(translation => translation.TranscriptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CaptionFile>(entity =>
        {
            entity.HasOne(captionFile => captionFile.Job)
                .WithMany()
                .HasForeignKey(captionFile => captionFile.JobId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(captionFile => captionFile.Transcript)
                .WithMany()
                .HasForeignKey(captionFile => captionFile.TranscriptId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(captionFile => captionFile.Translation)
                .WithMany(translation => translation.CaptionFiles)
                .HasForeignKey(captionFile => captionFile.TranslationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasOne(job => job.Project)
                .WithMany(project => project.Jobs)
                .HasForeignKey(job => job.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHash)
                .IsUnique();

            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CombinedMedia>(entity =>
        {
            entity.Property(cm => cm.MuxType)
                .HasMaxLength(50);

            entity.Property(cm => cm.Status)
                .HasMaxLength(50);

            entity.HasOne(cm => cm.Project)
                .WithMany()
                .HasForeignKey(cm => cm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.MediaFile)
                .WithMany()
                .HasForeignKey(cm => cm.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
