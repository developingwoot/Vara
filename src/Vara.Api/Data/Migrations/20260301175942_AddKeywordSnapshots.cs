using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "idx_keyword_snapshots_user_captured",
                table: "keyword_snapshots",
                columns: new[] { "user_id", "captured_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "keyword_snapshots");
        }
    }
}
