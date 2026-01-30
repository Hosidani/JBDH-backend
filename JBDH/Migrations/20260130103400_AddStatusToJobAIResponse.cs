using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JBDH.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToJobAIResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JobAIResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobAIResponses");
        }
    }
}
