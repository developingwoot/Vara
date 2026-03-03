using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Episode12_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the 2-column index and replace with a 3-column index that includes feature,
            // so queries filtering by (user_id, billing_period, feature) hit a covering index.
            migrationBuilder.DropIndex(
                name: "idx_usage_logs_user_period",
                table: "usage_logs");

            migrationBuilder.CreateIndex(
                name: "idx_usage_logs_user_period_feature",
                table: "usage_logs",
                columns: new[] { "user_id", "billing_period", "feature" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_usage_logs_user_period_feature",
                table: "usage_logs");

            migrationBuilder.CreateIndex(
                name: "idx_usage_logs_user_period",
                table: "usage_logs",
                columns: new[] { "user_id", "billing_period" });
        }
    }
}
