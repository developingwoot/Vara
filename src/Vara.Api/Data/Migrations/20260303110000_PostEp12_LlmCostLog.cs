using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostEp12_LlmCostLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "llm_cost_logs",
                columns: table => new
                {
                    id               = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id          = table.Column<Guid>(type: "uuid", nullable: false),
                    task_type        = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider         = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model            = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_tokens    = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost_usd         = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    billing_period   = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at       = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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

            migrationBuilder.CreateIndex(
                name: "idx_llm_cost_logs_user_period",
                table: "llm_cost_logs",
                columns: ["user_id", "billing_period"]);

            migrationBuilder.CreateIndex(
                name: "idx_llm_cost_logs_period",
                table: "llm_cost_logs",
                column: "billing_period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "llm_cost_logs");
        }
    }
}
