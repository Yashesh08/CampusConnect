using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedOpportunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saved_opportunities",
                columns: table => new
                {
                    saved_opportunity_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    student_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    saved_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_opportunities", x => x.saved_opportunity_id);
                    table.ForeignKey(
                        name: "FK_saved_opportunities_opportunities_opportunity_id",
                        column: x => x.opportunity_id,
                        principalTable: "opportunities",
                        principalColumn: "opportunity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_opportunities_student_profiles_student_id",
                        column: x => x.student_id,
                        principalTable: "student_profiles",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_opportunities_opportunity_id",
                table: "saved_opportunities",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_opportunities_student_id_opportunity_id",
                table: "saved_opportunities",
                columns: new[] { "student_id", "opportunity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_opportunities");
        }
    }
}
