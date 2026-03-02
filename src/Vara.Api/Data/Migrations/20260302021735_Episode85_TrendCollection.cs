using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Episode85_TrendCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "idx_seed_keywords_niche",
                table: "seed_keywords",
                column: "niche");

            migrationBuilder.CreateIndex(
                name: "unique_seed_keyword_niche",
                table: "seed_keywords",
                columns: new[] { "keyword", "niche" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "keyword_volume_history");

            migrationBuilder.DropTable(
                name: "seed_keywords");
        }
    }
}
