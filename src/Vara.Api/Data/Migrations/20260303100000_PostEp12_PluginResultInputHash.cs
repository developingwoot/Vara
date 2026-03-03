using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostEp12_PluginResultInputHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "input_hash",
                table: "plugin_results",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_plugin_results_input_hash",
                table: "plugin_results",
                columns: ["user_id", "plugin_id", "input_hash"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_plugin_results_input_hash",
                table: "plugin_results");

            migrationBuilder.DropColumn(
                name: "input_hash",
                table: "plugin_results");
        }
    }
}
