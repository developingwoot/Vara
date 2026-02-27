using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoAndKeyword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    channel_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "videos");
        }
    }
}
