using Microsoft.EntityFrameworkCore;
using Vara.Api.Models.Entities;

namespace Vara.Api.Data;

public class VaraContext : DbContext
{
    public VaraContext(DbContextOptions<VaraContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<KeywordSnapshot> KeywordSnapshots => Set<KeywordSnapshot>();
    public DbSet<TrackedChannel> TrackedChannels => Set<TrackedChannel>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
    public DbSet<SeedKeyword> SeedKeywords => Set<SeedKeyword>();
    public DbSet<KeywordVolumeHistory> KeywordVolumeHistory => Set<KeywordVolumeHistory>();
    public DbSet<PluginMetadata> PluginMetadata => Set<PluginMetadata>();
    public DbSet<PluginResult> PluginResults => Set<PluginResult>();
    public DbSet<CanonicalNiche> CanonicalNiches => Set<CanonicalNiche>();
    public DbSet<LlmCostLog> LlmCostLogs => Set<LlmCostLog>();

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
            entity.Property(e => e.IsAdmin)
                  .HasColumnName("is_admin")
                  .HasDefaultValue(false);

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

        modelBuilder.Entity<KeywordSnapshot>(entity =>
        {
            entity.ToTable("keyword_snapshots");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Keyword).HasColumnName("keyword").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Niche).HasColumnName("niche").HasMaxLength(100);
            entity.Property(e => e.SearchVolumeRelative).HasColumnName("search_volume_relative");
            entity.Property(e => e.CompetitionScore).HasColumnName("competition_score");
            entity.Property(e => e.CapturedAt)
                  .HasColumnName("captured_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.CapturedAt })
                  .HasDatabaseName("idx_keyword_snapshots_user_captured");
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
            entity.Property(e => e.NicheRaw).HasColumnName("niche_raw");
            entity.Property(e => e.NicheId).HasColumnName("niche_id");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Niche)
                  .WithMany()
                  .HasForeignKey(e => e.NicheId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.UserId, e.YoutubeChannelId })
                  .IsUnique()
                  .HasDatabaseName("unique_user_channel");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_tracked_channels_user_id");
        });

        modelBuilder.Entity<CanonicalNiche>(entity =>
        {
            entity.ToTable("canonical_niches");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).HasColumnName("slug").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Aliases).HasColumnName("aliases").HasColumnType("text[]");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("unique_canonical_niche_slug");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_canonical_niches_active");
        });

        modelBuilder.Entity<UsageLog>(entity =>
        {
            entity.ToTable("usage_logs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Feature).HasColumnName("feature").IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitCount).HasColumnName("unit_count").HasDefaultValue(1);
            entity.Property(e => e.BillingPeriod).HasColumnName("billing_period");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.BillingPeriod, e.Feature })
                  .HasDatabaseName("idx_usage_logs_user_period_feature");
        });

        modelBuilder.Entity<SeedKeyword>(entity =>
        {
            entity.ToTable("seed_keywords");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();

            entity.Property(e => e.Keyword).HasColumnName("keyword").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Niche).HasColumnName("niche").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasColumnName("category").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).HasColumnName("priority").HasDefaultValue(100);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.Keyword, e.Niche })
                  .IsUnique()
                  .HasDatabaseName("unique_seed_keyword_niche");
            entity.HasIndex(e => e.Niche).HasDatabaseName("idx_seed_keywords_niche");
        });

        modelBuilder.Entity<KeywordVolumeHistory>(entity =>
        {
            entity.ToTable("keyword_volume_history");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();

            entity.Property(e => e.Keyword).HasColumnName("keyword").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Niche).HasColumnName("niche").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Volume).HasColumnName("volume");
            entity.Property(e => e.Source).HasColumnName("source").IsRequired().HasMaxLength(20).HasDefaultValue("seed");
            entity.Property(e => e.RecordedDate).HasColumnName("recorded_date");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.Keyword, e.Niche, e.RecordedDate, e.Source })
                  .IsUnique()
                  .HasDatabaseName("unique_keyword_volume_date");
            entity.HasIndex(e => e.RecordedDate).HasDatabaseName("idx_kvh_recorded_date");
        });

        modelBuilder.Entity<PluginMetadata>(entity =>
        {
            entity.ToTable("plugin_metadata");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.PluginId).HasColumnName("plugin_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.Author).HasColumnName("author");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Tier).HasColumnName("tier").HasDefaultValue("free");
            entity.Property(e => e.Enabled).HasColumnName("enabled").HasDefaultValue(true);
            entity.Property(e => e.PluginDirectory).HasColumnName("plugin_directory");
            entity.Property(e => e.UnitsPerRun).HasColumnName("units_per_run");
            entity.Property(e => e.DiscoveredAt)
                  .HasColumnName("discovered_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.PluginId).IsUnique().HasDatabaseName("unique_plugin_id");
        });

        modelBuilder.Entity<PluginResult>(entity =>
        {
            entity.ToTable("plugin_results");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.PluginId).HasColumnName("plugin_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ResultDataJson)
                  .HasColumnName("result_data")
                  .HasColumnType("jsonb")
                  .HasDefaultValueSql("'{}'");
            entity.Property(e => e.InputHash).HasColumnName("input_hash").HasMaxLength(64);
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_plugin_results_user_id");
            entity.HasIndex(e => e.PluginId).HasDatabaseName("idx_plugin_results_plugin_id");
            entity.HasIndex(e => new { e.UserId, e.PluginId, e.InputHash })
                  .HasDatabaseName("idx_plugin_results_input_hash");
        });

        modelBuilder.Entity<LlmCostLog>(entity =>
        {
            entity.ToTable("llm_cost_logs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TaskType).HasColumnName("task_type").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).HasColumnName("provider").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Model).HasColumnName("model").IsRequired().HasMaxLength(100);
            entity.Property(e => e.PromptTokens).HasColumnName("prompt_tokens");
            entity.Property(e => e.CompletionTokens).HasColumnName("completion_tokens");
            entity.Property(e => e.CostUsd).HasColumnName("cost_usd").HasColumnType("numeric(10,6)");
            entity.Property(e => e.BillingPeriod).HasColumnName("billing_period");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.BillingPeriod })
                  .HasDatabaseName("idx_llm_cost_logs_user_period");
            entity.HasIndex(e => e.BillingPeriod)
                  .HasDatabaseName("idx_llm_cost_logs_period");
        });
    }
}
