using Microsoft.EntityFrameworkCore;
using Vara.Api.Models.Entities;

namespace Vara.Api.Data;

public class VaraContext : DbContext
{
    public VaraContext(DbContextOptions<VaraContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<TrackedChannel> TrackedChannels => Set<TrackedChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SubscriptionTier)
                  .HasColumnName("subscription_tier")
                  .HasDefaultValue("free");
            entity.Property(e => e.SubscriptionExpiresAt).HasColumnName("subscription_expires_at");
            entity.Property(e => e.Settings)
                  .HasColumnName("settings")
                  .HasColumnType("jsonb")
                  .HasDefaultValueSql("'{}'");

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("idx_users_email");
            entity.HasIndex(e => e.SubscriptionTier).HasDatabaseName("idx_users_subscription_tier");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.ToTable("videos");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.YoutubeId).HasColumnName("youtube_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ChannelName).HasColumnName("channel_name");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.UploadDate).HasColumnName("upload_date");
            entity.Property(e => e.ViewCount).HasColumnName("view_count").HasDefaultValue(0L);
            entity.Property(e => e.LikeCount).HasColumnName("like_count").HasDefaultValue(0);
            entity.Property(e => e.CommentCount).HasColumnName("comment_count").HasDefaultValue(0);
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.TranscriptText).HasColumnName("transcript_text");
            entity.Property(e => e.MetadataFetchedAt).HasColumnName("metadata_fetched_at");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.YoutubeId })
                  .IsUnique()
                  .HasDatabaseName("unique_user_video");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_videos_user_id");
            entity.HasIndex(e => e.UploadDate).HasDatabaseName("idx_videos_upload_date");
        });

        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.ToTable("keywords");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Text).HasColumnName("keyword");
            entity.Property(e => e.Niche).HasColumnName("niche");
            entity.Property(e => e.SearchVolumeRelative).HasColumnName("search_volume_relative");
            entity.Property(e => e.CompetitionScore).HasColumnName("competition_score");
            entity.Property(e => e.TrendDirection).HasColumnName("trend_direction");
            entity.Property(e => e.KeywordIntent).HasColumnName("keyword_intent");
            entity.Property(e => e.LastAnalyzed).HasColumnName("last_analyzed");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.Text, e.Niche })
                  .IsUnique()
                  .HasDatabaseName("unique_user_keyword");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_keywords_user_id");
            entity.HasIndex(e => e.TrendDirection).HasDatabaseName("idx_keywords_trend");
        });

        modelBuilder.Entity<TrackedChannel>(entity =>
        {
            entity.ToTable("tracked_channels");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.YoutubeChannelId).HasColumnName("youtube_channel_id");
            entity.Property(e => e.Handle).HasColumnName("handle");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.SubscriberCount).HasColumnName("subscriber_count");
            entity.Property(e => e.VideoCount).HasColumnName("video_count");
            entity.Property(e => e.TotalViewCount).HasColumnName("total_view_count");
            entity.Property(e => e.IsOwner).HasColumnName("is_owner").HasDefaultValue(false);
            entity.Property(e => e.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            entity.Property(e => e.LastSyncedAt).HasColumnName("last_synced_at");
            entity.Property(e => e.AddedAt)
                  .HasColumnName("added_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.YoutubeChannelId })
                  .IsUnique()
                  .HasDatabaseName("unique_user_channel");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_tracked_channels_user_id");
        });
    }
}
