using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JBDH.Migrations
{
    /// <inheritdoc />
    public partial class DropRawJobsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawJobs"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawJobs",
                columns: table => new
                {
                    JobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySql.EntityFrameworkCore.Metadata.MySQLValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    Company = table.Column<string>(type: "longtext", nullable: true),
                    Location = table.Column<string>(type: "longtext", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WebpageUrl = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawJobs", x => x.JobId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }
    }
}
