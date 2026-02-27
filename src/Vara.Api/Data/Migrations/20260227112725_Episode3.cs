using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Episode3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "channel_id",
                table: "videos",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

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
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracked_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_tracked_channels_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tracked_channels_user_id",
                table: "tracked_channels",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_user_channel",
                table: "tracked_channels",
                columns: new[] { "user_id", "youtube_channel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tracked_channels");

            migrationBuilder.DropColumn(
                name: "channel_id",
                table: "videos");
        }
    }
}
