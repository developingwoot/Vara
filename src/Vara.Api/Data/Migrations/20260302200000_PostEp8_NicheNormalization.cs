#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Vara.Api.Data.Migrations;

[DbContext(typeof(VaraContext))]
[Migration("20260302200000_PostEp8_NicheNormalization")]
public partial class PostEp8_NicheNormalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // canonical_niches reference table
        migrationBuilder.CreateTable(
            name: "canonical_niches",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                aliases = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_canonical_niches", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "unique_canonical_niche_slug",
            table: "canonical_niches",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_canonical_niches_active",
            table: "canonical_niches",
            column: "is_active");

        // Add niche columns to tracked_channels
        migrationBuilder.AddColumn<string>(
            name: "niche_raw",
            table: "tracked_channels",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "niche_id",
            table: "tracked_channels",
            type: "integer",
            nullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_tracked_channels_canonical_niches_niche_id",
            table: "tracked_channels",
            column: "niche_id",
            principalTable: "canonical_niches",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateIndex(
            name: "IX_tracked_channels_niche_id",
            table: "tracked_channels",
            column: "niche_id");

        // Add is_admin to users
        migrationBuilder.AddColumn<bool>(
            name: "is_admin",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_tracked_channels_canonical_niches_niche_id",
            table: "tracked_channels");

        migrationBuilder.DropIndex(
            name: "IX_tracked_channels_niche_id",
            table: "tracked_channels");

        migrationBuilder.DropColumn(name: "niche_raw", table: "tracked_channels");
        migrationBuilder.DropColumn(name: "niche_id", table: "tracked_channels");
        migrationBuilder.DropColumn(name: "is_admin", table: "users");

        migrationBuilder.DropTable(name: "canonical_niches");
    }
}
