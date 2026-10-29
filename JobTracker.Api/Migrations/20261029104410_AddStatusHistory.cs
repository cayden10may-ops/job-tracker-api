using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatusChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    JobApplicationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusChanges_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatusChanges_JobApplicationId",
                table: "StatusChanges",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatusChanges");
        }
    }
}
