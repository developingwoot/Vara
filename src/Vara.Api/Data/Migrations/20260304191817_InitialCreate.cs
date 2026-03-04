using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "canonical_niches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_canonical_niches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keyword_volume_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    keyword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    niche = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    volume = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "seed"),
                    recorded_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keyword_volume_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plugin_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plugin_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    author = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "free"),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    plugin_directory = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    units_per_run = table.Column<int>(type: "integer", nullable: true),
                    discovered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugin_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plugin_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plugin_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                    input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugin_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seed_keywords",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    keyword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    niche = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    subscription_tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "free"),
                    subscription_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settings = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                    RefreshToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keyword_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    niche = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    search_volume_relative = table.Column<short>(type: "smallint", nullable: false),
                    competition_score = table.Column<short>(type: "smallint", nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keyword_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_keyword_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    niche = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    search_volume_relative = table.Column<short>(type: "smallint", nullable: true),
                    competition_score = table.Column<short>(type: "smallint", nullable: true),
                    trend_direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    keyword_intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_analyzed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keywords", x => x.id);
                    table.ForeignKey(
                        name: "FK_keywords_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "llm_cost_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    billing_period = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_cost_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_llm_cost_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracked_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_channel_id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    handle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    subscriber_count = table.Column<long>(type: "bigint", nullable: true),
                    video_count = table.Column<int>(type: "integer", nullable: true),
                    total_view_count = table.Column<long>(type: "bigint", nullable: true),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    niche_raw = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    niche_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracked_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_tracked_channels_canonical_niches_niche_id",
                        column: x => x.niche_id,
                        principalTable: "canonical_niches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tracked_channels_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    billing_period = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_usage_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    channel_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    channel_id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    thumbnail_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    transcript_text = table.Column<string>(type: "text", nullable: true),
                    metadata_fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_videos", x => x.id);
                    table.ForeignKey(
                        name: "FK_videos_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "youtube_oauth_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_channel_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_oauth_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_youtube_oauth_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_canonical_niches_active",
                table: "canonical_niches",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "unique_canonical_niche_slug",
                table: "canonical_niches",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_keyword_snapshots_user_captured",
                table: "keyword_snapshots",
                columns: new[] { "user_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "idx_kvh_recorded_date",
                table: "keyword_volume_history",
                column: "recorded_date");

            migrationBuilder.CreateIndex(
                name: "unique_keyword_volume_date",
                table: "keyword_volume_history",
                columns: new[] { "keyword", "niche", "recorded_date", "source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_keywords_trend",
                table: "keywords",
                column: "trend_direction");

            migrationBuilder.CreateIndex(
                name: "idx_keywords_user_id",
                table: "keywords",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_user_keyword",
                table: "keywords",
                columns: new[] { "user_id", "keyword", "niche" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_llm_cost_logs_period",
                table: "llm_cost_logs",
                column: "billing_period");

            migrationBuilder.CreateIndex(
                name: "idx_llm_cost_logs_user_period",
                table: "llm_cost_logs",
                columns: new[] { "user_id", "billing_period" });

            migrationBuilder.CreateIndex(
                name: "unique_plugin_id",
                table: "plugin_metadata",
                column: "plugin_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_plugin_results_input_hash",
                table: "plugin_results",
                columns: new[] { "user_id", "plugin_id", "input_hash" });

            migrationBuilder.CreateIndex(
                name: "idx_plugin_results_plugin_id",
                table: "plugin_results",
                column: "plugin_id");

            migrationBuilder.CreateIndex(
                name: "idx_plugin_results_user_id",
                table: "plugin_results",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_seed_keywords_niche",
                table: "seed_keywords",
                column: "niche");

            migrationBuilder.CreateIndex(
                name: "unique_seed_keyword_niche",
                table: "seed_keywords",
                columns: new[] { "keyword", "niche" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tracked_channels_user_id",
                table: "tracked_channels",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracked_channels_niche_id",
                table: "tracked_channels",
                column: "niche_id");

            migrationBuilder.CreateIndex(
                name: "unique_user_channel",
                table: "tracked_channels",
                columns: new[] { "user_id", "youtube_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_usage_logs_user_period_feature",
                table: "usage_logs",
                columns: new[] { "user_id", "billing_period", "feature" });

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_subscription_tier",
                table: "users",
                column: "subscription_tier");

            migrationBuilder.CreateIndex(
                name: "idx_videos_upload_date",
                table: "videos",
                column: "upload_date");

            migrationBuilder.CreateIndex(
                name: "idx_videos_user_id",
                table: "videos",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_user_video",
                table: "videos",
                columns: new[] { "user_id", "youtube_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "unique_youtube_oauth_user",
                table: "youtube_oauth_tokens",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "keyword_snapshots");

            migrationBuilder.DropTable(
                name: "keyword_volume_history");

            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "llm_cost_logs");

            migrationBuilder.DropTable(
                name: "plugin_metadata");

            migrationBuilder.DropTable(
                name: "plugin_results");

            migrationBuilder.DropTable(
                name: "seed_keywords");

            migrationBuilder.DropTable(
                name: "tracked_channels");

            migrationBuilder.DropTable(
                name: "usage_logs");

            migrationBuilder.DropTable(
                name: "videos");

            migrationBuilder.DropTable(
                name: "youtube_oauth_tokens");

            migrationBuilder.DropTable(
                name: "canonical_niches");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
