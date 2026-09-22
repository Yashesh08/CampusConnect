using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunitySkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opportunities",
                columns: table => new
                {
                    opportunity_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    organizer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    target_department_id = table.Column<int>(type: "INTEGER", nullable: true),
                    work_mode = table.Column<int>(type: "INTEGER", nullable: false),
                    stipend_salary = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    registration_deadline = table.Column<DateTime>(type: "TEXT", nullable: false),
                    event_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    approval_status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunities", x => x.opportunity_id);
                    table.ForeignKey(
                        name: "FK_opportunities_AspNetUsers_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_opportunities_departments_target_department_id",
                        column: x => x.target_department_id,
                        principalTable: "departments",
                        principalColumn: "department_id");
                });

            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    student_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    applied_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    organizer_remarks = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applications", x => x.application_id);
                    table.ForeignKey(
                        name: "FK_applications_opportunities_opportunity_id",
                        column: x => x.opportunity_id,
                        principalTable: "opportunities",
                        principalColumn: "opportunity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_applications_student_profiles_student_id",
                        column: x => x.student_id,
                        principalTable: "student_profiles",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_skills",
                columns: table => new
                {
                    opportunity_skill_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    opportunity_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    skill_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_skills", x => x.opportunity_skill_id);
                    table.ForeignKey(
                        name: "FK_opportunity_skills_opportunities_opportunity_id",
                        column: x => x.opportunity_id,
                        principalTable: "opportunities",
                        principalColumn: "opportunity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_opportunity_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "skill_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_applications_opportunity_id",
                table: "applications",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_applications_student_id",
                table: "applications",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_organizer_id",
                table: "opportunities",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_target_department_id",
                table: "opportunities",
                column: "target_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_skills_opportunity_id",
                table: "opportunity_skills",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_skills_skill_id",
                table: "opportunity_skills",
                column: "skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "opportunity_skills");

            migrationBuilder.DropTable(
                name: "opportunities");
        }
    }
}
