using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace JBDH.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserIntoUserAndAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    AccountEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    AccountPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    AccountType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Basic"),
                    IsTerminated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    TerminatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AcceptedTermsAndConditions = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    TermsAcceptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
