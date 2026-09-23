using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyAndGrievances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_profiles_departments_department_id",
                table: "student_profiles");

            migrationBuilder.CreateTable(
                name: "faculty_office_hours",
                columns: table => new
                {
                    slot_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    faculty_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    end_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_booked = table.Column<bool>(type: "INTEGER", nullable: false),
                    booked_by_student_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faculty_office_hours", x => x.slot_id);
                    table.ForeignKey(
                        name: "FK_faculty_office_hours_AspNetUsers_faculty_user_id",
                        column: x => x.faculty_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_faculty_office_hours_student_profiles_booked_by_student_id",
                        column: x => x.booked_by_student_id,
                        principalTable: "student_profiles",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "faculty_profiles",
                columns: table => new
                {
                    faculty_profile_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    department_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    cabin_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faculty_profiles", x => x.faculty_profile_id);
                    table.ForeignKey(
                        name: "FK_faculty_profiles_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_faculty_profiles_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grievances",
                columns: table => new
                {
                    grievance_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    complainant_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_anonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    department_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    sla_due_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grievances", x => x.grievance_id);
                    table.ForeignKey(
                        name: "FK_grievances_AspNetUsers_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_grievances_AspNetUsers_complainant_user_id",
                        column: x => x.complainant_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_grievances_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grievance_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    grievance_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    status_changed_to = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resolution_note = table.Column<string>(type: "TEXT", nullable: true),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grievance_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_grievance_logs_AspNetUsers_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_grievance_logs_grievances_grievance_id",
                        column: x => x.grievance_id,
                        principalTable: "grievances",
                        principalColumn: "grievance_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_faculty_office_hours_booked_by_student_id",
                table: "faculty_office_hours",
                column: "booked_by_student_id");

            migrationBuilder.CreateIndex(
                name: "IX_faculty_office_hours_faculty_user_id",
                table: "faculty_office_hours",
                column: "faculty_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_faculty_profiles_department_id",
                table: "faculty_profiles",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_faculty_profiles_user_id",
                table: "faculty_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grievance_logs_grievance_id",
                table: "grievance_logs",
                column: "grievance_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievance_logs_updated_by_user_id",
                table: "grievance_logs",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_assigned_to_user_id",
                table: "grievances",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_complainant_user_id",
                table: "grievances",
                column: "complainant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_department_id",
                table: "grievances",
                column: "department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_student_profiles_departments_department_id",
                table: "student_profiles",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_profiles_departments_department_id",
                table: "student_profiles");

            migrationBuilder.DropTable(
                name: "faculty_office_hours");

            migrationBuilder.DropTable(
                name: "faculty_profiles");

            migrationBuilder.DropTable(
                name: "grievance_logs");

            migrationBuilder.DropTable(
                name: "grievances");

            migrationBuilder.AddForeignKey(
                name: "FK_student_profiles_departments_department_id",
                table: "student_profiles",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
