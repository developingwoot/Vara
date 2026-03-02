using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Episode8_UsageLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "idx_usage_logs_user_period",
                table: "usage_logs",
                columns: new[] { "user_id", "billing_period" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usage_logs");
        }
    }
}
